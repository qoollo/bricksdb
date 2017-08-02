﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ninject;
using Qoollo.Impl.Common.NetResults.System.Writer;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Modules;
using Qoollo.Impl.TestSupport;
using Qoollo.Impl.Writer.AsyncDbWorks.Restore;
using Qoollo.Impl.Writer.AsyncDbWorks.Support;
using Qoollo.Impl.Writer.AsyncDbWorks.Timeout;
using Qoollo.Impl.Writer.Interfaces;

namespace Qoollo.Impl.Writer.AsyncDbWorks
{
    internal class AsyncDbWorkModule : ControlModule, IAsyncDbWorkModule
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        public RestoreState RestoreState => _stateHolder.State;

        public TimeoutModule TimeoutModule => _timeout;

        internal bool IsNeedRestore => _stateHolder.State != RestoreState.Restored;

        public bool IsRestoreStarted => _initiatorRestore.IsStart || _broadcastRestore.IsStart;

        public Dictionary<string, string> FullState
        {
            get
            {
                var dictionary = new Dictionary<string, string>();

                if (_initiatorRestore.IsStart)
                {
                    var server = _initiatorRestore.RestoreServer;
                    if (server != null)
                        dictionary.Add(ServerState.RestoreCurrentServer, server.ToString());
                    else
                        dictionary.Add(ServerState.RestoreInProcess, _initiatorRestore.IsStart.ToString());
                }

                if (_transferRestore.IsStart)
                {
                    var server = _transferRestore.RemoteServer;
                    if (server != null)
                        dictionary.Add(ServerState.RestoreTransferServer, server.ToString());
                    else
                        dictionary.Add(ServerState.RestoreTransferInProcess, _transferRestore.IsStart.ToString());
                    if (!string.IsNullOrEmpty(_transferRestore.LastStartedTime))
                        dictionary.Add(ServerState.RestoreTransferLastStart, _transferRestore.LastStartedTime);
                }
                return dictionary;
            }
        }

        public AsyncDbWorkModule(StandardKernel kernel, bool needRestore = false)
            : base(kernel)
        {
            _stateHolder = new RestoreStateHolder(needRestore);
            _saver = LoadRestoreStateFromFile();
        }

        private IWriterModel _writerModel;

        private BroadcastRestoreModule _broadcastRestore;
        private InitiatorRestoreModule _initiatorRestore;
        private TransferRestoreModule _transferRestore;
        private TimeoutModule _timeout;

        private RestoreStateHolder _stateHolder;
        private readonly RestoreStateFileLogger _saver;

        public override void Start()
        {
            _writerModel = Kernel.Get<IWriterModel>();

            _initiatorRestore = new InitiatorRestoreModule(Kernel, _stateHolder, _saver);
            _transferRestore = new TransferRestoreModule(Kernel);
            _timeout = new TimeoutModule(Kernel);
            _broadcastRestore = new BroadcastRestoreModule(Kernel);

            _initiatorRestore.Start();
            _transferRestore.Start();
            _broadcastRestore.Start();

            _timeout.Start();

            if (_saver.IsNeedRestore())
            {
                Task.Delay(Consts.StartRestoreTimeout).ContinueWith(task =>
                {
                    //Todo broadcast
                    RestoreFromFile(_saver.RestoreServers);
                });
            }
        }

        public void UpdateModel()
        {
            _initiatorRestore.UpdateModel(_writerModel.Servers);
            _stateHolder.ModelUpdate();

            _saver.Save();
        }

        #region Restore process
        
        public void RestoreIncome(ServerId server, RestoreState state, string tableName)
        {
            _transferRestore.Restore(server, state == RestoreState.FullRestoreNeed, tableName);
        }

        public void PeriodMessageIncome(ServerId server)
        {
            _initiatorRestore.PeriodMessageIncome(server);
        }
        
        public void LastMessageIncome(ServerId server)
        {            
            _initiatorRestore.LastMessageIncome(server);
        }

        private RestoreStateFileLogger LoadRestoreStateFromFile()
        {
            var saver = new RestoreStateFileLogger(InitInjection.RestoreHelpFile);
            if (!saver.Load())
                return new RestoreStateFileLogger(InitInjection.RestoreHelpFile, _stateHolder);
            
            _stateHolder = saver.StateHolder;
            return saver;
        }

        #endregion

        #region Restore start 

        public void Restore(RestoreFromDistributorCommand comm)
        {
            var state = comm.RestoreState;
            var type = comm.Type;
            var destServers = comm.Server == null ? null : new List<ServerId> {comm.Server};

            Restore(state, type, destServers);
        }

        public void Restore(RestoreCommand comm)
        {
            var state = comm.RestoreState;
            var type = comm.Type;
            var destServers = comm.DirectServers;

            Restore(state, type, destServers);
        }

        public void Restore(RestoreState state, RestoreType type, List<ServerId> destServers)
        {
            if (_logger.IsWarnEnabled)
                _logger.Warn(
                    $"Attempt to start restore state: {Enum.GetName(typeof(RestoreState), state)}, type: {Enum.GetName(typeof(RestoreType), type)}",
                    "restore");

            var st = state;
            if (state == RestoreState.Default && type == RestoreType.Single)
            {
                st = RestoreState;
                if (st == RestoreState.Restored)
                {
                    if (_logger.IsWarnEnabled)
                        _logger.Warn(
                            $"Cant run restore in {Enum.GetName(typeof(RestoreState), RestoreState.Restored)} state",
                            "restore");
                    return;
                }
            }

            if (destServers != null)
            {
                var servers = st == RestoreState.FullRestoreNeed
                    ? _writerModel.Servers
                    : _writerModel.OtherServers;

                RestoreRun(ServersOnDirectRestore(servers, destServers), st, type);
            }
            else if (type == RestoreType.Single || state == RestoreState.SimpleRestoreNeed)
            {
                var servers = st == RestoreState.FullRestoreNeed
                    ? _writerModel.Servers
                    : _writerModel.OtherServers;

                RestoreRun(ConvertRestoreServers(servers), st, type);
            }
            else if (type == RestoreType.Broadcast)
            {
                RestoreRun(ConvertRestoreServers(_writerModel.Servers), st, type);
            }
        }

        private void RestoreRun(List<RestoreServer> servers, RestoreState state, RestoreType type)
        {
            if (type == RestoreType.Single)
            {
                if (_initiatorRestore.IsStart)
                    return;

                _initiatorRestore.Restore(servers, state, Consts.AllTables);
                _saver.Save();
            }
            if (type == RestoreType.Broadcast)
            {
                if (_broadcastRestore.IsStart)
                    return;

                _saver.SetRestoreDate(type, state, servers);
                _saver.Save();

                _broadcastRestore.Restore(servers, _stateHolder.State);                
            }
        }

        private void RestoreFromFile(List<RestoreServer> servers)
        {
            if (_initiatorRestore.IsStart)
                return;

            _initiatorRestore.RestoreFromFile(servers, _stateHolder.State, Consts.AllTables);
        }

        private List<RestoreServer> ServersOnDirectRestore(List<ServerId> servers, List<ServerId> failedServers)
        {
            return servers.Select(x =>
            {
                var ret = new RestoreServer(x, _writerModel.GetHashMap(x));
                if (failedServers.Contains(x))
                    ret.NeedRestoreInitiate();
                return ret;
            }).ToList();
        }

        private List<RestoreServer> ConvertRestoreServers(IEnumerable<ServerId> servers)
        {
            return servers.Select(x =>
            {
                var ret = new RestoreServer(x, _writerModel.GetHashMap(x));
                ret.NeedRestoreInitiate();
                return ret;
            }).ToList();
        }

        #endregion

        #region Support

        public List<ServerId> GetFailedServers()
        {
            return _initiatorRestore.FailedServers;
        }

        public List<RestoreServer> Servers => _initiatorRestore.Servers;

        public RestoreState DistributorReceive(RestoreState state)
        {
            var old = _stateHolder.State;
            _stateHolder.DistributorSendState(state);

            if (old != _stateHolder.State)
                _saver.Save();

            return _stateHolder.State;
        }

        public string GetAllState()
        {
            string result = string.Empty;

            result += $"restore state: {Enum.GetName(typeof(RestoreState), RestoreState)}\n";
            result += $"restore is running: {_initiatorRestore.IsStart}\n";

            if (_initiatorRestore.IsStart)
            {
                result += $"current server: {GetCurrentRestoreServer()}\n";
                result += $"servers:{GetServersList()}\n";
            }

            result += $"restore transfer is running: {_transferRestore.IsStart}\n";

            if (_transferRestore.IsStart)
                result += $"transfert server: {_transferRestore.RemoteServer}\n";

            return result;
        }

        private string GetCurrentRestoreServer()
        {
            var server = _initiatorRestore.RestoreServer;
            if (server != null)
                return server.ToString();
            return string.Empty;
        }

        private string GetServersList(string start = "\n")
        {
            return Servers.Aggregate(start, (current, server) => current + $"\t{server}\n");
        }

        #endregion

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {                
                _broadcastRestore.Dispose();
                _transferRestore.Dispose();
                _initiatorRestore.Dispose();
                _timeout.Dispose();
            }

            base.Dispose(isUserCall);
        }
    }
}

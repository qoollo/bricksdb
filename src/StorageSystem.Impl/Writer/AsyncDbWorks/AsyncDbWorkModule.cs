using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.TestSupport;
using Qoollo.Impl.Writer.AsyncDbWorks.Restore;
using Qoollo.Impl.Writer.AsyncDbWorks.Support;
using Qoollo.Impl.Writer.AsyncDbWorks.Timeout;
using Qoollo.Impl.Writer.Db;
using Qoollo.Impl.Writer.WriterNet;

namespace Qoollo.Impl.Writer.AsyncDbWorks
{
    internal class AsyncDbWorkModule:ControlModule
    {
        public RestoreState RestoreState => _stateHolder.State;

        public TimeoutModule TimeoutModule => _timeout;

        internal bool IsNeedRestore => _stateHolder.State != RestoreState.Restored;

        public bool IsRestoreStarted => _initiatorRestore.IsStart;

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

        public AsyncDbWorkModule(
            WriterModel writerModel,
            WriterNetModule writerNet, 
            AsyncTaskModule async, 
            DbModuleCollection db,
            RestoreModuleConfiguration initiatorConfiguration,
            RestoreModuleConfiguration transferConfiguration,
            RestoreModuleConfiguration timeoutConfiguration,
            QueueConfiguration queueConfiguration, 
            bool isNeedRestore = false)
        {
            Contract.Requires(writerModel != null);
            Contract.Requires(initiatorConfiguration != null);
            Contract.Requires(transferConfiguration != null);
            Contract.Requires(timeoutConfiguration != null);
            Contract.Requires(queueConfiguration != null);
            Contract.Requires(db != null);
            Contract.Requires(writerNet != null);
            Contract.Requires(async != null);

            _writerModel = writerModel;

            _stateHolder = new RestoreStateHolder(isNeedRestore);
            _saver = LoadRestoreStateFromFile();

            _initiatorRestore = new InitiatorRestoreModule(initiatorConfiguration, writerNet, async, 
                _stateHolder, _saver);

            _transferRestore = new TransferRestoreModule(writerModel, transferConfiguration, 
                writerNet, async, db,  queueConfiguration);

            _timeout = new TimeoutModule(writerNet, async, queueConfiguration,
                db,  timeoutConfiguration);

            _broadcastRestore = new BroadcastRestoreModule(writerModel, transferConfiguration,
                writerNet, async, db, queueConfiguration);

        }

        private readonly WriterModel _writerModel;

        private readonly BroadcastRestoreModule _broadcastRestore;
        private readonly InitiatorRestoreModule _initiatorRestore;
        private readonly TransferRestoreModule _transferRestore;
        private readonly TimeoutModule _timeout;

        private RestoreStateHolder _stateHolder;
        private readonly RestoreStateFileLogger _saver;

        public override void Start()
        {
            _initiatorRestore.Start();
            _transferRestore.Start();
            _broadcastRestore.Start();

            _timeout.Start();

            if (_saver.IsNeedRestore())
            {
                Task.Delay(Consts.StartRestoreTimeout).ContinueWith(task =>
                {
                    //Todo broadcast
                    RestoreFromFile(_writerModel.LocalMap, _saver.RestoreServers, _saver.TableName);
                });
            }
        }

        public void UpdateModel()
        {
            _initiatorRestore.UpdateModel(_writerModel.Servers);
            _stateHolder.LocalSendState(true);
            _saver.Save();
        }

        #region Restore process
        
        public void RestoreIncome(ServerId server, bool isSystemUpdated, string tableName)
        {
            _transferRestore.Restore(server, isSystemUpdated, tableName);
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

        public void Restore(List<RestoreServer> servers, RestoreState state, string tableName)
        {
            if (_initiatorRestore.IsStart)
                return;

            _stateHolder.LocalSendState(state);
            _initiatorRestore.Restore(_writerModel.LocalMap, servers, _stateHolder.State, tableName);
            _saver.Save();
        }

        public void Restore(List<RestoreServer> servers, RestoreState state)
        {
            Restore(servers, state, Consts.AllTables);
        }

        private void RestoreFromFile(List<HashMapRecord> local, List<RestoreServer> servers, string tableName)
        {
            if (_initiatorRestore.IsStart)
                return;

            _initiatorRestore.RestoreFromFile(local, servers, _stateHolder.State, tableName);
        }       

        #endregion

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

            result += $"restore state: {Enum.GetName(typeof (RestoreState), RestoreState)}\n";
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

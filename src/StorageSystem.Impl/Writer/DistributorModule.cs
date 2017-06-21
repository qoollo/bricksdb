using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.Data;
using Qoollo.Impl.Common.NetResults.System.Distributor;
using Qoollo.Impl.Common.NetResults.System.Writer;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.TestSupport;
using Qoollo.Impl.Writer.AsyncDbWorks;
using Qoollo.Impl.Writer.Db;
using Qoollo.Impl.Writer.Model;
using Qoollo.Impl.Writer.WriterNet;

namespace Qoollo.Impl.Writer
{
    internal class DistributorModule : ControlModule
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        private readonly WriterModel _model;
        private readonly WriterNetModule _writerNet;
        private readonly QueueConfiguration _queueConfiguration;
        private readonly DbModuleCollection _dbModuleCollection;
        private readonly AsyncDbWorkModule _asyncDbWork;
        private readonly GlobalQueueInner _queue;

        public DistributorModule(AsyncTaskModule async, AsyncDbWorkModule asyncDbWork,
            WriterNetModule writerNet,
            ServerId local,
            HashMapConfiguration hashMapConfiguration,
            QueueConfiguration configuration,
            DbModuleCollection dbModuleCollection,
            AsyncTasksConfiguration pingConfiguration = null)
        {
            Contract.Requires(local != null);
            Contract.Requires(writerNet != null);
            Contract.Requires(configuration != null);
            Contract.Requires(asyncDbWork != null);
            Contract.Requires(async != null);
            Contract.Requires(dbModuleCollection != null);

            _asyncDbWork = asyncDbWork;
            _model = new WriterModel(local, hashMapConfiguration);
            _writerNet = writerNet;
            _queueConfiguration = configuration;
            _dbModuleCollection = dbModuleCollection;
            _queue = GlobalQueue.Queue;

            var ping = InitInjection.PingPeriod;

            if (pingConfiguration != null)
                ping = pingConfiguration.TimeoutPeriod;

            async.AddAsyncTask(
                new AsyncDataPeriod(ping, Ping, AsyncTasksNames.AsyncPing, -1), false);
        }

        public override void Start()
        {
            _model.Start();
            _asyncDbWork.SetLocalHash(_model.LocalMap);
            RegistrateCommands();
        }

        private void RegistrateCommands()
        {
            RegistrateAsync<Transaction, Transaction, RemoteResult>(_queue.TransactionAnswerQueue, ProcessTransaction,
               () => new SuccessResult());

            RegistrateAsync<RestoreFromDistributorCommand, NetCommand, RemoteResult>(_queue.DbDistributorInnerQueue,
                RestoreCommand, () => new SuccessResult());

            RegistrateAsync<RestoreCommand, NetCommand, RemoteResult>(_queue.DbDistributorInnerQueue,
                RestoreCommand, () => new SuccessResult());

            RegistrateAsync<RestoreInProcessCommand, NetCommand, RemoteResult>(_queue.DbDistributorInnerQueue,
                command => _asyncDbWork.PeriodMessageIncome(command.ServerId), () => new SuccessResult());

            RegistrateAsync<RestoreCompleteCommand, NetCommand, RemoteResult>(_queue.DbDistributorInnerQueue,
                command => _asyncDbWork.LastMessageIncome(command.ServerId), () => new SuccessResult());

            RegistrateAsync<RestoreCommandWithData, NetCommand, RemoteResult>(_queue.DbDistributorInnerQueue,
                comm =>
                    _asyncDbWork.RestoreIncome(comm.ServerId, comm.RestoreState == RestoreState.FullRestoreNeed,
                        comm.Hash, comm.TableName, _model.LocalMap), () => new SuccessResult());

            RegistrateAsync<DeleteCommand, NetCommand, RemoteResult>(_queue.DbDistributorInnerQueue, DeleteCommand,
                () => new SuccessResult());

            RegistrateSync<SetGetRestoreStateCommand, SetGetRestoreStateResult>(
                command => new SetGetRestoreStateResult(
                    _asyncDbWork.DistributorReceive(command.State),
                    GetAllStateDict()));

            RegistrateSync<HashFileUpdateCommand, RemoteResult>(HashFileUpdate);

            StartAsync(_queueConfiguration);
        }

        private void Ping(AsyncData data)
        {
            var servers = _writerNet.GetServersByType(typeof (SingleConnectionToDistributor));
            _writerNet.PingDistributors(servers);

            servers = _writerNet.GetServersByType(typeof(SingleConnectionToWriter));
            _writerNet.PingWriter(servers);

            //remove old connections after model update
            var map = _model.Servers;
            servers = servers.Where(x => !map.Contains(x)).ToList();
            servers.ForEach(x => _writerNet.RemoveConnection(x));
        }

        #region to user

        public string UpdateModel()
        {
            if (!_asyncDbWork.IsRestoreStarted)
            {
                _model.UpdateModel();
                _asyncDbWork.SetLocalHash(_model.LocalMap);
                _asyncDbWork.UpdateModel(_model.Servers);

                return Errors.NoErrors;
            }
            return Errors.RestoreAlreadyStarted;
        }

        /// <summary>
        /// Start servers recover
        /// </summary>        
        /// <param name="state"></param>
        public string Restore(RestoreState state)
        {
            return Restore(null, state, Consts.AllTables);
        }

        public string Restore()
        {
            return Restore(null, RestoreState.Default, Consts.AllTables);
        }

        public string Restore(RestoreState state, string tableName)
        {
            return Restore(null, state, tableName);
        }

        public string Restore(List<ServerId> servers, RestoreState state)
        {
            return Restore(servers, state, Consts.AllTables);
        }

        public string Restore(List<ServerId> servers, RestoreState state, string tableName)
        {
            var ret = CheckRestoreArguments(tableName);

            if (ret != Errors.RestoreStartedWithoutErrors)
                return ret;

            RestoreState st = state;
            if (state == RestoreState.Default)
            {
                st = _asyncDbWork.RestoreState;
                if (st == RestoreState.Restored)
                    return Errors.RestoreDefaultStartError;
            }

            _queue.DbDistributorInnerQueue.Add(new RestoreCommand(_model.Local, tableName, st)
            {
                FailedServers = servers
            });

            return ret;
        }

        private string CheckRestoreArguments(string tableName)
        {
            if (_asyncDbWork.IsRestoreStarted)
                return Errors.RestoreAlreadyStarted;

            if (tableName != Consts.AllTables && !_dbModuleCollection.GetDbModules.Exists(x => x.TableName == tableName))
                return Errors.TableDoesNotExists;

            return Errors.RestoreStartedWithoutErrors;
        }

        public bool IsRestoreCompleted()
        {
            return !_asyncDbWork.IsRestoreStarted;
        }

        public List<ServerId> FailedServers()
        {
            return _asyncDbWork.GetFailedServers();
        }

        public string GetCurrentRestoreServer()
        {
            var server = _asyncDbWork.GetRestoreServer();
            if (server != null)
                return server.ToString();
            return string.Empty;
        }

        public RestoreState GetRestoreRequiredState()
        {
            return _asyncDbWork.RestoreState;
        }

        public string GetAllState()
        {
            string result = string.Empty;
            result += string.Format("restore state: {0}\n",
                Enum.GetName(typeof (RestoreState), GetRestoreRequiredState()));
            if (_asyncDbWork.IsRestoreStarted)
            {
                result += string.Format("current server: {0}\n", GetCurrentRestoreServer());
                result += "servers:\n";
                result = GetServersList(result);
            }
            else
                result += string.Format("restore is running: {0}\n", _asyncDbWork.IsRestoreStarted);

            if (_asyncDbWork.IsTransferRestoreStarted)
                result += string.Format("transfert server: {0}\n", _asyncDbWork.GetTransferServer());
            else
                result += string.Format("restore transfer is running: {0}\n", _asyncDbWork.IsTransferRestoreStarted);

            return result;
        }

        private string GetServersList(string start = "\n")
        {
            return _asyncDbWork.Servers.Aggregate(start,
                (current, server) => current + string.Format("\t{0}\n", server));
        }

        private Dictionary<string, string> GetAllStateDict()
        {
            var ret = _asyncDbWork.FullState;
            if (GetRestoreRequiredState() != RestoreState.Restored)
                ret.Add(ServerState.RestoreServers, GetServersList());
            return ret;
        }

        public string DisableDelete()
        {
            _asyncDbWork.TimeoutModule.Disable();
            return Errors.NoErrors;
        }

        public string EnableDelete()
        {
            _asyncDbWork.TimeoutModule.Enable();
            return Errors.NoErrors;
        }

        public string StartDelete()
        {
            _asyncDbWork.TimeoutModule.StartDelete();
            return Errors.NoErrors;
        }

        public string RunDelete()
        {
            _asyncDbWork.TimeoutModule.RunDelete();
            return Errors.NoErrors;
        }

        #endregion

        #region Commands

        private void DeleteCommand(DeleteCommand command)
        {
            switch (command.Command.ToLower())
            {
                case "disable":
                    DisableDelete();
                    break;
                case "enable":
                    EnableDelete();
                    break;
                case "start":
                    StartDelete();
                    break;
                case "run":
                    RunDelete();
                    break;
            }
        }

        private List<RestoreServer> ConvertRestoreServers(IEnumerable<ServerId> servers)
        {
            return servers.Select(x =>
            {
                var ret = new RestoreServer(x);
                ret.NeedRestoreInitiate();
                return ret;
            }).ToList();
        }

        private List<RestoreServer> ServersOnDirectRestore(RestoreState state, List<ServerId> failedServers)
        {
            var servers = state == RestoreState.FullRestoreNeed
                ? _model.Servers
                : _model.Servers.Where(x => !x.Equals(_model.Local));

            return servers.Select(x =>
            {
                var ret = new RestoreServer(x);
                if (failedServers.Contains(x))
                    ret.NeedRestoreInitiate();
                return ret;
            }).ToList();
        }

        private void RestoreCommand(RestoreCommand comm)
        {
            if (_logger.IsDebugEnabled)
                _logger.Debug($"First restore server = {_model.Servers.Count(x => !x.Equals(_model.Local))}",
                    "restore");

            var st = comm.RestoreState;
            if (comm.RestoreState == RestoreState.Default)
            {
                st = _asyncDbWork.RestoreState;
                if (st == RestoreState.Restored)
                    return;
            }            

            if (comm.FailedServers != null)
            {
                _asyncDbWork.Restore(ServersOnDirectRestore(st, comm.FailedServers),
                    comm.RestoreState, comm.TableName);
            }
            else
            {
                var servers = st == RestoreState.FullRestoreNeed
                    ? _model.Servers
                    : _model.Servers.Where(x => !x.Equals(_model.Local));
                _asyncDbWork.Restore(ConvertRestoreServers(servers), st, comm.TableName);
            }
        }

        private void RestoreCommand(RestoreFromDistributorCommand comm)
        {
            var st = comm.RestoreState;
            if (comm.RestoreState == RestoreState.Default)
            {
                st = _asyncDbWork.RestoreState;
                if (st == RestoreState.Restored)
                    return;
            }

            if (comm.Server != null)
                _asyncDbWork.Restore(ServersOnDirectRestore(st, new List<ServerId> { comm.Server }), st);
            else
                _asyncDbWork.Restore(ConvertRestoreServers(_model.Servers.Where(x => !x.Equals(_model.Local))),
                    st);
        }

        private void ProcessTransaction(Transaction transaction)
        {
            if (transaction.Distributor != null)
                _writerNet.TransactionAnswer(transaction.Distributor, transaction);
        }

        private RemoteResult HashFileUpdate(HashFileUpdateCommand command)
        {
            if (_asyncDbWork.IsRestoreStarted)
                return new InnerFailResult("Restore process is started");

            var result = _model.UpdateHashViaNet(command.Map);
            RemoteResult ret;

            if (result == string.Empty)
            {
                ret = new SuccessResult();
                _asyncDbWork.SetLocalHash(_model.LocalMap);
                _asyncDbWork.UpdateModel(_model.Servers);
            }
            else
                ret = new InnerFailResult(result);

            return ret;
        }

        #endregion

        public bool IsMine(string hash)
        {
            return _model.IsMine(hash);
        }
    }
}

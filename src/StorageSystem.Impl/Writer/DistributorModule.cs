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

            _queue.DbDistributorInnerQueue.Registrate(new QueueConfiguration(1, 1000), ProcessInner);            
            _queue.TransactionAnswerQueue.Registrate(_queueConfiguration, ProcessTransaction);
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
            var ret = CheckRestoreArguments(Consts.AllTables);

            if (ret != Errors.RestoreStartedWithoutErrors)
                return ret;

            _queue.DbDistributorInnerQueue.Add(new RestoreCommand(_model.Local, Consts.AllTables, state));

            return ret;
        }

        public string Restore()
        {
            var ret = CheckRestoreArguments(Consts.AllTables);

            if (ret != Errors.RestoreStartedWithoutErrors)
                return ret;

            var state = _asyncDbWork.RestoreState;
            if (state == RestoreState.Restored)
                return Errors.RestoreDefaultStartError;

            _queue.DbDistributorInnerQueue.Add(new RestoreCommand(_model.Local, Consts.AllTables, state));

            return ret;
        }

        public string Restore(RestoreState state, string tableName)
        {
            var ret = CheckRestoreArguments(Consts.AllTables);

            if (ret != Errors.RestoreStartedWithoutErrors)
                return ret;

            _queue.DbDistributorInnerQueue.Add(new RestoreCommand(_model.Local, tableName, state));

            return ret;
        }

        public string Restore(List<ServerId> servers, RestoreState state)
        {
            var ret = CheckRestoreArguments(Consts.AllTables);

            if (ret != Errors.RestoreStartedWithoutErrors)
                return ret;

            _queue.DbDistributorInnerQueue.Add(new RestoreCommand(_model.Local, Consts.AllTables, state)
            {
                FailedServers = servers
            });

            return ret;
        }

        public string Restore(List<ServerId> servers, RestoreState state, string tableName)
        {
            var ret = CheckRestoreArguments(tableName);

            if (ret != Errors.RestoreStartedWithoutErrors)
                return ret;

            _queue.DbDistributorInnerQueue.Add(new RestoreCommand(_model.Local, tableName, state)
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
            result += string.Format("restore is running: {0}\n", _asyncDbWork.IsRestoreStarted);
            if (_asyncDbWork.IsRestoreStarted)
            {
                result += string.Format("current server: {0}\n", GetCurrentRestoreServer());
                result += "servers:\n";
                result = _asyncDbWork.Servers.Aggregate(result,
                    (current, server) => current + string.Format("\t{0}\n", server));
            }

            result += string.Format("restore transfer is running: {0}\n", _asyncDbWork.IsTransferRestoreStarted);
            if (_asyncDbWork.IsTransferRestoreStarted)
                result += _asyncDbWork.GetTransferServer();

            return result;
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

        #endregion

        #region Commands

        public RemoteResult ProcessSend(NetCommand command)
        {
            if (command is SetGetRestoreStateCommand)
            {
                var ret = new SetGetRestoreStateResult(
                    _asyncDbWork.DistributorReceive(((SetGetRestoreStateCommand) command).State),
                    _asyncDbWork.FullState);
                return ret;
            }

            if (command is HashFileUpdateCommand)
                return HashFileUpdate(command as HashFileUpdateCommand);

            _queue.DbDistributorInnerQueue.Add(command);
            return new SuccessResult();
        }

        private void ProcessInner(NetCommand command)
        {
            if (command is RestoreFromDistributorCommand)
            {
                _asyncDbWork.Restore(_model.Servers.Where(x => !x.Equals(_model.Local)).ToList(),
                    RestoreState.SimpleRestoreNeed);
            }
            else if (command is RestoreCommand)
            {
                var comm = command as RestoreCommand;
                Logger.Logger.Instance.Debug(
                    string.Format("First restore server = {0}", _model.Servers.Count(x => !x.Equals(_model.Local))),
                    "restore");

                if (comm.FailedServers != null)
                {
                    var list = _model.Servers.Where(x => comm.FailedServers.Contains(x)).ToList();
                    _asyncDbWork.Restore(list, comm.RestoreState, comm.TableName);
                }
                else
                {
                    var servers = comm.RestoreState == RestoreState.FullRestoreNeed
                        ? _model.Servers
                        : _model.Servers.Where(x => !x.Equals(_model.Local));
                    _asyncDbWork.Restore(servers.ToList(), comm.RestoreState, comm.TableName);
                }
            }
            else if (command is RestoreInProcessCommand)
                _asyncDbWork.PeriodMessageIncome(((RestoreInProcessCommand) command).ServerId);
            else if (command is RestoreCompleteCommand)
                _asyncDbWork.LastMessageIncome(((RestoreCompleteCommand) command).ServerId);
            else if (command is RestoreCommandWithData)
            {
                var comm = command as RestoreCommandWithData;
                _asyncDbWork.RestoreIncome(comm.ServerId, comm.RestoreState == RestoreState.FullRestoreNeed, comm.Hash,
                    comm.TableName, _model.LocalMap);
            }
            else
                Logger.Logger.Instance.ErrorFormat("Not supported command {0}", command.GetType());
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

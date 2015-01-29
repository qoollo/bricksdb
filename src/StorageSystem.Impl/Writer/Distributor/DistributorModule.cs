using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.System.Distributor;
using Qoollo.Impl.Common.NetResults.System.Writer;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.Writer.AsyncDbWorks;
using Qoollo.Impl.Writer.Db;
using Qoollo.Impl.Writer.Model;
using Qoollo.Impl.Writer.WriterNet;

namespace Qoollo.Impl.Writer.Distributor
{
    internal class DistributorModule : ControlModule
    {
        private WriterModel _model;
        private WriterNetModule _writerNet;
        private QueueConfiguration _queueConfiguration;
        private DbModuleCollection _dbModuleCollection;
        private AsyncTaskModule _async;
        private AsyncDbWorkModule _asyncDbWork;
        private GlobalQueueInner _queue;

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
            Contract.Assert(dbModuleCollection != null);

            _async = async;
            _asyncDbWork = asyncDbWork;
            _model = new WriterModel(local, hashMapConfiguration);
            _writerNet = writerNet;
            _queueConfiguration = configuration;
            _dbModuleCollection = dbModuleCollection;
            _queue = GlobalQueue.Queue;

            var ping = TimeSpan.FromMinutes(1);

            if (pingConfiguration != null)
                ping = pingConfiguration.TimeoutPeriod;

            _async.AddAsyncTask(
                new AsyncDataPeriod(ping, Ping, AsyncTasksNames.AsyncPing, -1), false);
        }

        public override void Start()
        {
            _model.Start();

            _queue.DbDistributorInnerQueue.Registrate(new QueueConfiguration(1, 1000), ProcessInner);
            _queue.DbDistributorOuterQueue.Registrate(new QueueConfiguration(1, 1000), ProcessOuter);
            _queue.TransactionAnswerQueue.Registrate(_queueConfiguration, ProcessTransaction);
        }

        private void Ping(AsyncData data)
        {
            var servers = _writerNet.GetServersByType(typeof (SingleConnectionToDistributor));
            _writerNet.PingDistributors(servers);

            servers = _writerNet.GetServersByType(typeof(SingleConnectionToWriter));
            _writerNet.PingWriter(servers);
        }

        #region public

        public void UpdateModel()
        {
            if (!_asyncDbWork.IsStarted)
                _model.UpdateModel();
            else
            {
                //TODO вернуть что нельзя                
            }
        }

        /// <summary>
        /// Start servers recover
        /// </summary>
        /// <param name="server">Distributor address.</param>
        /// <param name="isModelUpdated">is hash file is new
        ///     true - need check all data
        ///     false - check only metatable</param>
        public string Restore(ServerId server, bool isModelUpdated)
        {
            var ret = CheckRestoreArguments(server, null, isModelUpdated, Consts.AllTables);

            if (ret != Errors.RestoreStartedWithoutErrors)
                return ret;

            _queue.DbDistributorInnerQueue.Add(new RestoreCommand(_model.Local, isModelUpdated, Consts.AllTables));
            _queue.DbDistributorOuterQueue.Add(new RestoreCommand(_model.Local, isModelUpdated, Consts.AllTables));

            return ret;
        }

        public string Restore(ServerId server, bool isModelUpdated, string tableName)
        {
            var ret = CheckRestoreArguments(server, null, isModelUpdated, Consts.AllTables);

            if (ret != Errors.RestoreStartedWithoutErrors)
                return ret;

            _queue.DbDistributorInnerQueue.Add(new RestoreCommand(_model.Local, isModelUpdated, tableName));
            _queue.DbDistributorOuterQueue.Add(new RestoreCommand(_model.Local, isModelUpdated, tableName));

            return ret;
        }

        public string Restore(ServerId server, List<ServerId> servers, bool isModelUpdated)
        {
            var ret = CheckRestoreArguments(server, servers, isModelUpdated, Consts.AllTables);

            if (ret != Errors.RestoreStartedWithoutErrors)
                return ret;

            _queue.DbDistributorInnerQueue.Add(new RestoreCommand(_model.Local, isModelUpdated, Consts.AllTables)
            {
                FailedServers = servers
            });
            _queue.DbDistributorOuterQueue.Add(new RestoreCommand(_model.Local, isModelUpdated, Consts.AllTables));

            return ret;
        }

        public string Restore(ServerId server, List<ServerId> servers, bool isModelUpdated, string tableName)
        {
            var ret = CheckRestoreArguments(server, servers, isModelUpdated, tableName);

            if (ret != Errors.RestoreStartedWithoutErrors)
                return ret;

            _queue.DbDistributorInnerQueue.Add(new RestoreCommand(_model.Local, isModelUpdated, tableName)
            {
                FailedServers = servers
            });
            _queue.DbDistributorOuterQueue.Add(new RestoreCommand(_model.Local, isModelUpdated, tableName));

            return ret;
        }

        private string CheckRestoreArguments(ServerId server, List<ServerId> servers, bool isModelUpdated,
            string tableName)
        {
            if (_asyncDbWork.IsStarted)
                return Errors.RestoreAlreadyStarted;

            if (!_writerNet.ConnectToDistributor(server))
                return Errors.RestoreFailConnectToDistributor;

            if (tableName != Consts.AllTables && !_dbModuleCollection.GetDbModules.Exists(x => x.TableName == tableName))
                return Errors.TableDoesNotExists;

            return Errors.RestoreStartedWithoutErrors;
        }

        public bool IsRestoreCompleted()
        {
            return _asyncDbWork.IsRestoreComplete();
        }

        public List<ServerId> FailedServers()
        {
            return _asyncDbWork.GetFailedServers();
        }

        #endregion

        #region Commands

        public RemoteResult ProcessSend(NetCommand command)
        {
            if (command is IsRestoredCommand)
                return new IsRestoredResult(_asyncDbWork.IsNeedRestore);

            _queue.DbDistributorInnerQueue.Add(command);
            return new SuccessResult();
        }

        private void ProcessInner(NetCommand command)
        {
            if (command is RestoreCommand)
            {
                var comm = command as RestoreCommand;
                Logger.Logger.Instance.Debug(
                    string.Format("First restore server = {0}", _model.Servers.Count(x => !x.Equals(_model.Local))),
                    "restore");

                if (comm.FailedServers != null)
                {
                    var list = _model.Servers.Where(x => comm.FailedServers.Contains(x)).ToList();
                    _asyncDbWork.Restore(_model.LocalMap, list, comm.IsModelUpdated, comm.TableName);
                }
                else
                {
                    var servers = comm.IsModelUpdated
                        ? _model.Servers
                        : _model.Servers.Where(x => !x.Equals(_model.Local));
                    _asyncDbWork.Restore(_model.LocalMap, servers.ToList(), comm.IsModelUpdated, comm.TableName);
                }
            }
            else if (command is RestoreInProcessCommand)
                _asyncDbWork.PeriodMessageIncome(((RestoreInProcessCommand) command).ServerId);
            else if (command is RestoreCompleteCommand)
                _asyncDbWork.LastMessageIncome(((RestoreCompleteCommand) command).ServerId);
            else if (command is RestoreCommandWithData)
            {
                var comm = command as RestoreCommandWithData;
                _asyncDbWork.RestoreIncome(comm.ServerId, comm.IsModelUpdated, comm.Hash, comm.TableName);
            }
            else
                Logger.Logger.Instance.ErrorFormat("Not supported command {0}", command.GetType());
        }

        private void ProcessOuter(NetCommand command)
        {
            if (command is RestoreCommand)
                _writerNet.SendToDistributor(command);
        }

        private void ProcessTransaction(Transaction transaction)
        {
            if (transaction.Distributor != null)
                _writerNet.TransactionAnswer(transaction.Distributor, transaction);
        }

        #endregion

        public bool IsMine(string hash)
        {
            return _model.IsMine(hash);
        }
    }
}

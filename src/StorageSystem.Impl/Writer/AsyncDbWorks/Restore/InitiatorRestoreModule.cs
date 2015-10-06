using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.System.Writer;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Writer.AsyncDbWorks.Support;
using Qoollo.Impl.Writer.WriterNet;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Restore
{
    internal class InitiatorRestoreModule : CommonAsyncWorkModule
    {
        public ServerId RestoreServer
        {
            get
            {
                Lock.EnterReadLock();
                var server = _remoteServer;
                Lock.ExitReadLock();
                return server;
            }
        }

        public List<ServerId> FailedServers
        {
            get
            {
                Lock.EnterReadLock();
                var ret = _failServers != null ? new List<ServerId>(_failServers) : new List<ServerId>();
                Lock.ExitReadLock();
                return ret;
            }
        }

        public InitiatorRestoreModule(RestoreModuleConfiguration configuration, WriterNetModule writerNet,
            AsyncTaskModule asyncTaskModule, RestoreStateHelper stateHelper) : base(writerNet, asyncTaskModule)
        {
            Contract.Requires(configuration != null);
            Contract.Requires(stateHelper != null);
            _configuration = configuration;
            _stateHelper = stateHelper;
            _failServers = new List<ServerId>();
        }

        private List<HashMapRecord> _local;
        private Dictionary<ServerId, bool> _servers;
        private ServerId _remoteServer;
        private readonly RestoreModuleConfiguration _configuration;
        private readonly RestoreStateHelper _stateHelper;
        private bool _isModelUpdated;
        private string _tableName;
        private List<ServerId> _failServers;

        public bool Restore(List<HashMapRecord> local, List<ServerId> servers, bool isModelUpdated, string tableName)
        {
            if (!(servers.Count > 0 && local.Count > 0) || IsStart)
                return false;

            _isModelUpdated = isModelUpdated;
            _tableName = tableName;
            IsStart = true;
            _local = local;

            _failServers = new List<ServerId>();
            _servers = servers.Select(x => new { Key = x, Value = false }).ToDictionary(x => x.Key, x => x.Value);

            AsyncTaskModule.AddAsyncTask(
                new AsyncDataPeriod(_configuration.PeriodRetry, NoAnswerCallback, AsyncTasksNames.RestoreRemote,
                    _configuration.CountRetry), false);

            AsyncTaskModule.StopTask(AsyncTasksNames.RestoreRemote);

            CurrentProcess();

            return true;
        }

        public bool Restore(List<HashMapRecord> local, List<ServerId> servers, bool isModelUpdated)
        {
            return Restore(local, servers, isModelUpdated, Consts.AllTables);
        }

        public void UpdateModel(List<ServerId> servers)
        {
            Lock.EnterWriteLock();
            _failServers.RemoveAll(servers.Contains);
            if(servers.Contains(_remoteServer))
                _remoteServer = null;
            Lock.ExitWriteLock();
        }

        private void CurrentProcess()
        {
            int result;
            do
            {
                result = ProcessNextServer();

                //TODO all servers are processed
                if (result == -1 && FailedServers.Count == 0)
                    FinishRestore();
                else if (result == -1)
                {
                    ProcessFailedServers();
                    result = 1;
                }

            } while (result == 1);            

            if (result != -1)
                AsyncTaskModule.RestartTask(AsyncTasksNames.RestoreRemote);
        }

        private int ProcessNextServer()
        {
            var next = _servers.FirstOrDefault(x => !x.Value);
            if (next.Equals(default(KeyValuePair<ServerId, bool>)))
                return -1;

            bool result = WriterNet.ConnectToWriter(next.Key);

            Logger.Logger.Instance.Trace(string.Format("Connection result = {0}", result), "restore");
            var ret = WriterNet.SendToWriter(next.Key,
                new RestoreCommandWithData(_local[0].ServerId,
                    _local.ToList(), !next.Key.Equals(_local[0].ServerId) && _isModelUpdated, _tableName));

            _servers[next.Key] = true;

            if (ret is FailNetResult)
            {
                Logger.Logger.Instance.InfoFormat(
                    "Restore command for server: {0} failed with result: {1}", next.Key, ret.Description);
                AddServerToFailed(next.Key);
                return 1;
            }

            Lock.EnterWriteLock();
            _remoteServer = next.Key;
            _failServers.Remove(_remoteServer);
            Lock.ExitWriteLock();

            return 0;
        }

        private void FinishRestore()
        {
            AsyncTaskModule.DeleteTask(AsyncTasksNames.RestoreRemote);
            IsStart = false;
            _stateHelper.FinishRestore(_isModelUpdated);
            Logger.Logger.Instance.Info("Restore completed");
        }

        private void ProcessFailedServers()
        {            
            _servers = FailedServers.Select(x => new {Key = x, Value = false}).ToDictionary(x => x.Key, x => x.Value);            
            AsyncTaskModule.StopTask(AsyncTasksNames.RestoreRemote);            
        }

        #region Period events

        /// <summary>
        /// Message from server, indicates that he is alive and sending data
        /// </summary>
        /// <param name="server"></param>
        public void PeriodMessageIncome(ServerId server)
        {
            Logger.Logger.Instance.Trace(string.Format("period messge income from {0}", server), "restore");
                        
            if (server.Equals(RestoreServer))
                AsyncTaskModule.RestartTask(AsyncTasksNames.RestoreRemote);
        }

        /// <summary>
        /// Message from servers? indicates that he sent all data
        /// </summary>
        /// <param name="server"></param>
        public void LastMessageIncome(ServerId server)
        {
            Logger.Logger.Instance.Debug(string.Format("last message income from {0}", server), "restore");

            if (server.Equals(RestoreServer))
                CurrentProcess();
        }
     
        private void NoAnswerCallback(AsyncData async)
        {
            AsyncTaskModule.StopTask(AsyncTasksNames.RestoreRemote);

            var remoteServer = RestoreServer;

            if (async.IsLast() || remoteServer == null)
            {
                if (RestoreServer != null)
                    AddServerToFailed(RestoreServer);
                CurrentProcess();
                return;
            }
            
            var ret = WriterNet.SendToWriter(remoteServer, new RestoreCommandWithData(_local[0].ServerId,
                _local.ToList(), _isModelUpdated, _tableName));

            if (ret is FailNetResult)
            {
                if (RestoreServer!=null)
                    AddServerToFailed(RestoreServer);
                CurrentProcess();
            }
            else
                AsyncTaskModule.StartTask(AsyncTasksNames.RestoreRemote);
        }

        #endregion

        private void AddServerToFailed(ServerId server)
        {
            Lock.EnterWriteLock();
            
            if (!_failServers.Contains(server))
                _failServers.Add(server);

            Lock.ExitWriteLock();
        }

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                AsyncTaskModule.StopTask(AsyncTasksNames.RestoreRemote);
            }

            base.Dispose(isUserCall);
        }
    }
}

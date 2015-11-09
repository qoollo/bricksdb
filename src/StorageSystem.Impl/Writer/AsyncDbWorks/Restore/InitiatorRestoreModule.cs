﻿using System;
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
                var server = _restoreServers.FirstOrDefault(x=>x.IsCurrentServer);
                Lock.ExitReadLock();
                return server;
            }
        }

        public List<ServerId> FailedServers
        {
            get
            {
                Lock.EnterReadLock();
                var ret = _restoreServers.Where(x => x.IsFailed).Select(x => (ServerId)x).ToList();
                Lock.ExitReadLock();
                return ret;
            }
        }

        public InitiatorRestoreModule(RestoreModuleConfiguration configuration, WriterNetModule writerNet,
            AsyncTaskModule asyncTaskModule, RestoreStateHelper stateHelper)
            : base(writerNet, asyncTaskModule)
        {
            Contract.Requires(configuration != null);
            Contract.Requires(stateHelper != null);
            _configuration = configuration;
            _stateHelper = stateHelper;
            _restoreServers = new List<RestoreServer>();
        }

        private List<HashMapRecord> _local;        
        private readonly RestoreModuleConfiguration _configuration;
        private readonly RestoreStateHelper _stateHelper;
        private bool _isModelUpdated;
        private string _tableName;
        private List<RestoreServer> _restoreServers;

        public void Restore(List<HashMapRecord> local, List<ServerId> servers, bool isModelUpdated, string tableName)
        {
            Lock.EnterWriteLock();

            try
            {
                if (IsStartNoLock || !(servers.Count > 0 && local.Count > 0))
                    return;

                _isModelUpdated = isModelUpdated;
                _tableName = tableName;
                IsStartNoLock = true;
                _local = local;
            }
            finally
            {
                Lock.ExitWriteLock();
            }

            _restoreServers = servers.Select(x =>
            {
                var s = new RestoreServer(x);
                s.NeedRestoreInitiate();
                return s;
            }).ToList();

            AsyncTaskModule.AddAsyncTask(
                new AsyncDataPeriod(_configuration.PeriodRetry, NoAnswerCallback, AsyncTasksNames.RestoreRemote,
                    _configuration.CountRetry), false);

            AsyncTaskModule.StopTask(AsyncTasksNames.RestoreRemote);

            CurrentProcess();
        }

        public void Restore(List<HashMapRecord> local, List<ServerId> servers, bool isModelUpdated)
        {
            Restore(local, servers, isModelUpdated, Consts.AllTables);
        }

        public void UpdateModel(List<ServerId> servers)
        {
            Lock.EnterWriteLock();

            _restoreServers.ForEach(x => x.IsFailed = false);

            foreach (var server in servers)
            {
                var s = _restoreServers.FirstOrDefault(x => x.Equals(server));

                if(s != null && !s.IsCurrentServer)
                    _restoreServers.Remove(s);
            }

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
            var nextServer = _restoreServers.FirstOrDefault(x => x.IsNeedCurrentRestore());
            if (nextServer == null)
                return -1;

            bool result = WriterNet.ConnectToWriter(nextServer);

            Logger.Logger.Instance.Trace(string.Format("Connection result = {0}", result), "restore");
            var ret = WriterNet.SendToWriter(nextServer,
                new RestoreCommandWithData(_local[0].ServerId, _local.ToList(),
                    !nextServer.Equals(_local[0].ServerId) && _isModelUpdated, _tableName));

            nextServer.IsRestored = true;

            if (ret is FailNetResult)
            {
                Logger.Logger.Instance.InfoFormat(
                    "Restore command for server: {0} failed with result: {1}", nextServer, ret.Description);
                AddServerToFailed(nextServer);
                return 1;
            }
            
            Lock.EnterWriteLock();
                        
            ChangeCurrentServer();
            nextServer.IsFailed = false;
            nextServer.IsCurrentServer = true;

            Lock.ExitWriteLock();

            return 0;
        }

        private void FinishRestore()
        {
            AsyncTaskModule.DeleteTask(AsyncTasksNames.RestoreRemote);
            IsStart = false;
            _stateHelper.FinishRestore(_isModelUpdated);
            ChangeCurrentServer();
            Logger.Logger.Instance.Info("Restore current servers complete");
        }

        private void ProcessFailedServers()
        {
            foreach (var server in FailedServers)
            {
                var s = _restoreServers.FirstOrDefault(x => x.Equals(server));
                if (s != null)
                    s.AfterFailed();
            }
            AsyncTaskModule.StopTask(AsyncTasksNames.RestoreRemote);
        }

        private void ChangeCurrentServer()
        {
            var servers = _restoreServers.FirstOrDefault(x => x.IsCurrentServer);
            if (servers != null)
                servers.IsCurrentServer = false;
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
                if (RestoreServer != null)
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

            var s = _restoreServers.FirstOrDefault(x => x.Equals(server));
            if (s != null)
                s.IsFailed = true;

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

﻿using System.Collections.Generic;
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
using Qoollo.Impl.Writer.WriterNet;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Restore
{
    internal class InitiatorRestoreModule:CommonAsyncWorkModule
    {
        private List<HashMapRecord> _local;
        private Dictionary<ServerId, bool> _servers;
        private ServerId _remoteServer;
        private readonly RestoreModuleConfiguration _configuration;
        private bool _isModelUpdated;
        private string _tableName;

        public InitiatorRestoreModule(RestoreModuleConfiguration configuration, WriterNetModule writerNet,
            AsyncTaskModule asyncTaskModule) : base(writerNet, asyncTaskModule)
        {
            Contract.Requires(configuration != null);
            _configuration = configuration;
            _lock = new ReaderWriterLockSlim();
        }

        #region Logic

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

            StartNextServer();

            return true;
        }

        public bool Restore(List<HashMapRecord> local, List<ServerId> servers, bool isModelUpdated)
        {
            return Restore(local, servers, isModelUpdated, Consts.AllTables);
        }

        /// <summary>
        /// Message from server, indicates that he is alive and sending data
        /// </summary>
        /// <param name="server"></param>
        public void PeriodMessageIncome(ServerId server)
        {
            //Logger.Logger.Instance.Debug("period messge income", "restore");
            if (server.Equals(_remoteServer))
                AsyncTaskModule.RestartTask(AsyncTasksNames.RestoreRemote);
        }

        /// <summary>
        /// Message from servers? indicates that he sent all data
        /// </summary>
        /// <param name="server"></param>
        public void LastMessageIncome(ServerId server)
        {
            Logger.Logger.Instance.Debug("last message income", "restore");
            if (server.Equals(_remoteServer))
                StartNextServer();
        }

        private void StartNextServer()
        {
            int result;
            do
            {
                result = SendNextServer();

            } while (result == 1);

            if (result == -1)
            {
                //TODO all servers are processed
                AsyncTaskModule.DeleteTask(AsyncTasksNames.RestoreRemote);
                IsStart = false;                
                Logger.Logger.Instance.Info("Restore completed");
            }
            else
                AsyncTaskModule.RestartTask(AsyncTasksNames.RestoreRemote);
        }

        private int SendNextServer()
        {
            var next = _servers.FirstOrDefault(x => !x.Value);
            if (next.Equals(default(KeyValuePair<ServerId, bool>)))
                return -1;

            bool  result = WriterNet.ConnectToWriter(next.Key);

            Logger.Logger.Instance.Debug(string.Format("Connection result = {0}", result), "restore");
            var ret = WriterNet.SendToWriter(next.Key,
                new RestoreCommandWithData(_local[0].ServerId,
                    _local.ToList(), !next.Key.Equals(_local[0].ServerId) && _isModelUpdated, _tableName));
           
            _servers[next.Key] = true;

            if (ret is FailNetResult)
            {
                Logger.Logger.Instance.ErrorFormat(
                    "Restore command for server: {0} failed with result: {1}", next.Key, ret.Description);                
                AddServerToFailed(next.Key);
                return 1;
            }
            _remoteServer = next.Key;

            return 0;
        }

        private void NoAnswerCallback(AsyncData async)
        {
            AsyncTaskModule.StopTask(AsyncTasksNames.RestoreRemote);

            if (async.IsLast())
            {
                AddServerToFailed(_remoteServer);
                StartNextServer();
                return;
            }

            var ret = WriterNet.SendToWriter(_remoteServer,
                                            new RestoreCommandWithData(_local[0].ServerId,
                                                                       _local.ToList(), _isModelUpdated, _tableName));

            if (ret is FailNetResult)
            {
                AddServerToFailed(_remoteServer);
                StartNextServer();
            }
            else
                AsyncTaskModule.StartTask(AsyncTasksNames.RestoreRemote);
        }

        #endregion

        #region lock

        private List<ServerId> _failServers;

        public List<ServerId> FailedServers
        {
            get
            {
                List<ServerId> ret;
                _lock.EnterReadLock();
                ret = _failServers!=null ? new List<ServerId>(_failServers) : new List<ServerId>();
                _lock.ExitReadLock();
                return ret;
            }
        }

        private ReaderWriterLockSlim _lock;

        private void AddServerToFailed(ServerId server)
        {
            _lock.EnterWriteLock();
             _failServers.Add(server);
            _lock.ExitWriteLock();
        }

        #endregion

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

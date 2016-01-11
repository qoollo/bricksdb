using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.NetResults.System.Writer;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Writer.Db;
using Qoollo.Impl.Writer.WriterNet;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Restore
{
    internal class TransferRestoreModule : CommonAsyncWorkModule
    {
        public ServerId RemoteServer
        {
            get
            {
                Lock.EnterReadLock();
                var server = _remoteServer;
                Lock.ExitReadLock();
                return server;
            }
        }

        public string LastStartedTime
        {
            get
            {
                try
                {
                    Lock.EnterReadLock();
                    return _lastDateTime;                    
                }
                finally
                {
                    Lock.ExitReadLock();
                }
            }
        }

        public TransferRestoreModule(RestoreModuleConfiguration configuration, WriterNetModule writerNet,
            AsyncTaskModule asyncTaskModule, DbModuleCollection db, ServerId local,
            QueueConfiguration queueConfiguration)
            : base(writerNet, asyncTaskModule)
        {
            Contract.Requires(configuration != null);
            Contract.Requires(db != null);
            Contract.Requires(local != null);
            Contract.Requires(queueConfiguration != null);

            _db = db;
            _configuration = configuration;
            _local = local;
            _queueConfiguration = queueConfiguration;
            _lastDateTime = string.Empty;
        }

        private readonly RestoreModuleConfiguration _configuration;
        private readonly DbModuleCollection _db;
        private readonly ServerId _local;
        private ServerId _remoteServer;
        private readonly QueueConfiguration _queueConfiguration;
        private RestoreProcess _restore;
        private string _lastDateTime;

        public void RestoreIncome(ServerId remoteServer, bool isSystemUpdated,
            List<KeyValuePair<string, string>> remoteHashRange, string tableName,
            List<HashMapRecord> localHashRange)
        {
            Lock.EnterWriteLock();
            try
            {
                if (IsStartNoLock)
                    return;

                Logger.Logger.Instance.Debug(string.Format("transafer start {0}, {1}", remoteServer, remoteHashRange),
                    "restore");

                IsStartNoLock = true;
                _remoteServer = remoteServer;
                _lastDateTime = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            }
            finally
            {
                Lock.ExitWriteLock();
            }


            AsyncTaskModule.AddAsyncTask(
                new AsyncDataPeriod(_configuration.PeriodRetry, RestoreAnswerCallback, AsyncTasksNames.RestoreLocal,
                    -1), false);

            _restore = new RestoreProcess(remoteHashRange, localHashRange, isSystemUpdated, _db, _queueConfiguration,
                tableName, WriterNet, _remoteServer);
        }        

        private void RestoreAnswerCallback(AsyncData obj)
        {
            if (_restore == null)
                return;

            Logger.Logger.Instance.Debug(string.Format("Async complete = {0}, start = {1}",
                _restore.IsComplete, IsStart), "restore");

            if (_restore.IsComplete && IsStart)
            {
                AsyncTaskModule.DeleteTask(AsyncTasksNames.RestoreLocal);

                WriterNet.SendToWriter(_remoteServer, new RestoreCompleteCommand(_local));
                _restore.Dispose();
                IsStart = false;
            }
            else
            {
                if (_restore.IsQueueEmpty && IsStart)
                    _restore.GetAnotherData();

                WriterNet.SendToWriter(_remoteServer, new RestoreInProcessCommand(_local));
            }
        }

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                AsyncTaskModule.StopTask(AsyncTasksNames.RestoreLocal);
                if (_restore != null)
                    _restore.Dispose();
            }

            base.Dispose(isUserCall);
        }
    }
}

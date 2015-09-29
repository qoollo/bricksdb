using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
        private readonly RestoreModuleConfiguration _configuration;
        private readonly DbModuleCollection _db;
        private readonly ServerId _local;
        private ServerId _remote;
        private readonly QueueConfiguration _queueConfiguration;
        private RestoreProcess _restore;

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
        }

        public void RestoreIncome(ServerId remoteServer, bool isSystemUpdated,
            List<KeyValuePair<string, string>> remoteHashRange, string tableName,
            List<HashMapRecord> localHashRange)
        {
            if (IsStart)
                return;

            Logger.Logger.Instance.Debug(string.Format("transafer start {0}, {1}", remoteServer, remoteHashRange),
                "restore");

            IsStart = true;
            _remote = remoteServer;

            AsyncTaskModule.AddAsyncTask(
                new AsyncDataPeriod(_configuration.PeriodRetry, RestoreAnswerCallback, AsyncTasksNames.RestoreLocal,
                    -1), false);

            _restore = new RestoreProcess(remoteHashRange, localHashRange, isSystemUpdated, _db, _queueConfiguration,
                tableName, WriterNet, _remote);
        }

        private void RestoreAnswerCallback(AsyncData obj)
        {
            Logger.Logger.Instance.Debug(
                string.Format("Async complete = {0}, start = {1}", _restore.Reader.IsComplete, IsStart), "restore");

            if (_restore.Reader.IsComplete && IsStart)
            {
                AsyncTaskModule.DeleteTask(AsyncTasksNames.RestoreLocal);
                IsStart = false;

                WriterNet.SendToWriter(_remote, new RestoreCompleteCommand(_local));
                _restore.Dispose();
            }
            else
            {
                if (_restore.Reader.IsQueueEmpty && IsStart)
                    _restore.Reader.GetAnotherData();

                WriterNet.SendToWriter(_remote, new RestoreInProcessCommand(_local));
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

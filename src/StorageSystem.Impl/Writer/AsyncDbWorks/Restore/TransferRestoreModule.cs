using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using Qoollo.Impl.Common.NetResults.System.Writer;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Writer.AsyncDbWorks.Processes;
using Qoollo.Impl.Writer.Db;
using Qoollo.Impl.Writer.WriterNet;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Restore
{
    internal class TransferRestoreModule : CommonAsyncWorkModule
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

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

        public TransferRestoreModule(
            WriterModel writerModel, 
            RestoreModuleConfiguration configuration, 
            WriterNetModule writerNet,
            AsyncTaskModule asyncTaskModule, 
            DbModuleCollection db, 
            QueueConfiguration queueConfiguration)
            : base(writerNet, asyncTaskModule)
        {
            Contract.Requires(writerModel != null);
            Contract.Requires(configuration != null);
            Contract.Requires(db != null);
            Contract.Requires(queueConfiguration != null);

            _writerModel = writerModel;
            _db = db;
            _configuration = configuration;
            _queueConfiguration = queueConfiguration;
            _lastDateTime = string.Empty;
        }

        private readonly RestoreModuleConfiguration _configuration;
        private readonly WriterModel _writerModel;
        private readonly DbModuleCollection _db;
        private ServerId _remoteServer;
        private readonly QueueConfiguration _queueConfiguration;
        private SingleServerRestoreProcess _restore;
        private string _lastDateTime;

        public void RestoreIncome(ServerId remoteServer, bool isSystemUpdated, string tableName)
        {
            Lock.EnterWriteLock();
            try
            {
                if (IsStartNoLock)
                    return;

                if (_logger.IsInfoEnabled)
                    _logger.Info($"transafer start: {remoteServer}", "restore");

                IsStartNoLock = true;
                _remoteServer = remoteServer;
                _lastDateTime = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            }
            finally
            {
                Lock.ExitWriteLock();
            }

            AsyncTaskModule.AddAsyncTask(
                new AsyncDataPeriod(_configuration.PeriodRetry, RestoreAnswerCallback,
                    AsyncTasksNames.RestoreLocal, -1), false);

            _restore = new SingleServerRestoreProcess(_db, _writerModel, WriterNet, 
                tableName, _remoteServer, isSystemUpdated, _queueConfiguration);
            _restore.Start();
        }

        private void RestoreAnswerCallback(AsyncData obj)
        {
            if (_restore == null)
                return;

            if (_logger.IsDebugEnabled)
                _logger.Debug($"Async complete = {_restore.IsComplete}, start = {IsStart}", "restore");

            if (_restore.IsComplete && IsStart)
            {
                AsyncTaskModule.DeleteTask(AsyncTasksNames.RestoreLocal);

                WriterNet.SendToWriter(_remoteServer, new RestoreCompleteCommand(_writerModel.Local));
                _restore.Dispose();
                IsStart = false;
            }
            else
            {
                if (_restore.IsQueueEmpty && IsStart)
                    _restore.GetAnotherData();

                WriterNet.SendToWriter(_remoteServer, new RestoreInProcessCommand(_writerModel.Local));
            }
        }

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                AsyncTaskModule.StopTask(AsyncTasksNames.RestoreLocal);
                _restore?.Dispose();
            }

            base.Dispose(isUserCall);
        }
    }
}

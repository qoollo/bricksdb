﻿using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using Ninject;
using Qoollo.Impl.Common.NetResults.System.Writer;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Writer.AsyncDbWorks.Processes;
using Qoollo.Impl.Writer.Interfaces;

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

        public TransferRestoreModule(StandardKernel kernel, RestoreModuleConfiguration configuration)
            : base(kernel)
        {
            Contract.Requires(configuration != null);

            _configuration = configuration;
            _lastDateTime = string.Empty;
        }

        private readonly RestoreModuleConfiguration _configuration;
        private IWriterModel _writerModel;
        private IDbModule _db;
        private ServerId _remoteServer;
        private SingleServerRestoreProcess _restore;
        private string _lastDateTime;

        public override void Start()
        {
            base.Start();

            _db = Kernel.Get<IDbModule>();
            _writerModel = Kernel.Get<IWriterModel>();
        }

        public void Restore(ServerId remoteServer, bool isSystemUpdated, string tableName)
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

            _restore = new SingleServerRestoreProcess(Kernel, _db, _writerModel, WriterNet, 
                tableName, _remoteServer, isSystemUpdated);
            _restore.Start();

            AsyncTaskModule.AddAsyncTask(
                new AsyncDataPeriod(_configuration.PeriodRetry, RestoreAnswerCallback,
                    AsyncTasksNames.RestoreLocal, -1), false);
        }

        private void RestoreAnswerCallback(AsyncData obj)
        {
            if (_restore == null)
                return;

            if (_logger.IsDebugEnabled)
                _logger.Debug($"Async transfer complete: {_restore.IsComplete}, start: {IsStart}", "restore");

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

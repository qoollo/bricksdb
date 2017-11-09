﻿using System;
using Ninject;
using Qoollo.Impl.Common.NetResults.Data;
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

        public TransferRestoreModule(StandardKernel kernel): base(kernel)
        {
            _lastDateTime = DateTime.Now;
        }

        private IWriterModel _writerModel;
        private IDbModule _db;
        private ServerId _remoteServer;
        private SingleServerRestoreProcess _restore;
        private DateTime _lastDateTime;
        private IWriterConfiguration _config;
        private RestoreState _state;

        public override void Start()
        {
            base.Start();

            _db = Kernel.Get<IDbModule>();
            _writerModel = Kernel.Get<IWriterModel>();
            _config = Kernel.Get<IWriterConfiguration>();
        }

        public void Restore(ServerId remoteServer, RestoreState state, string tableName)
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
                _lastDateTime = DateTime.Now;
                _state = state;
            }
            finally
            {
                Lock.ExitWriteLock();
            }            

            _restore = new SingleServerRestoreProcess(Kernel, _db, _writerModel, WriterNet, 
                tableName, _remoteServer, state == RestoreState.FullRestoreNeed, 
                _config.Restore.Transfer.UsePackage);
            _restore.Start();

            AsyncTaskModule.AddAsyncTask(
                new AsyncDataPeriod(_config.Restore.Transfer.PeriodRetryMls, RestoreAnswerCallback,
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

                WriterNet.SendToWriter(_remoteServer, new RestoreCompleteCommand(_writerModel.Local, _state));
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

        public TransferStateDataContainer GetState()
        {
            if (IsStart)
                return new TransferStateDataContainer(_remoteServer, _lastDateTime);
            return null;
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

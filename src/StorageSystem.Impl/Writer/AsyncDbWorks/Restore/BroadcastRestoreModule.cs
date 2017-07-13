using System;
using System.Collections.Generic;
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
    internal class BroadcastRestoreModule: CommonAsyncWorkModule
    {
        private IWriterModel _writerModel;
        private readonly RestoreModuleConfiguration _configuration;
        private IDbModule _db;
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        private BroadcastRestoreProcess _restoreProcess;

        private string _lastDateTime;
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

        public BroadcastRestoreModule(StandardKernel kernel,RestoreModuleConfiguration configuration)
            : base(kernel)
        {
            _configuration = configuration;
            _lastDateTime = string.Empty;
        }

        public override void Start()
        {
            base.Start();

            _db = Kernel.Get<IDbModule>();
            _writerModel = Kernel.Get<IWriterModel>();
        }

        public void Restore(List<RestoreServer> servers, RestoreState state)
        {
            Lock.EnterWriteLock();
            try
            {
                if (IsStartNoLock)
                    return;

                IsStartNoLock = true;
                _lastDateTime = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            }
            finally
            {
                Lock.ExitWriteLock();
            }

            _restoreProcess = new BroadcastRestoreProcess(Kernel, _db, _writerModel, WriterNet, servers,
                state == RestoreState.FullRestoreNeed);
            _restoreProcess.Start();

            AsyncTaskModule.AddAsyncTask(
                new AsyncDataPeriod(_configuration.PeriodRetry, RestoreCheckStateCallback,
                    AsyncTasksNames.RestoreBroadcast, -1), false);
        }

        private void RestoreCheckStateCallback(AsyncData obj)
        {
            if (_restoreProcess == null)
                return;

            if (_logger.IsDebugEnabled)
                _logger.Debug($"Async broadcast complete: {_restoreProcess.IsComplete}, start: {IsStart}", "restore");

            if (_restoreProcess.IsComplete && IsStart)
            {
                AsyncTaskModule.DeleteTask(AsyncTasksNames.RestoreBroadcast);

                SendRestoreStatus();
                _restoreProcess.Dispose();

                IsStart = false;
            }
            else
            {
                if (_restoreProcess.IsQueueEmpty && IsStart)
                    _restoreProcess.GetAnotherData();
            }
        }

        private void SendRestoreStatus()
        {
            foreach (var serverId in _writerModel.Servers)
            {
                if (!_restoreProcess.FailedServers.Contains(serverId))
                {
                    WriterNet.SendToWriter(serverId, new RestoreCompleteCommand(_writerModel.Local));
                }
            }
        }

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                AsyncTaskModule.DeleteTask(AsyncTasksNames.RestoreBroadcast);
                _restoreProcess?.Dispose();
            }

            base.Dispose(isUserCall);
        }
    }
}
using System;
using Ninject;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.Writer.Interfaces;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Timeout
{
    internal class TimeoutModule:CommonAsyncWorkModule
    {
        private TimeoutReaderFull _reader;
        private IDbModule _db;
        private QueueWithParam<InnerData> _queue;
        private TimeSpan _deleteTimeout;
        private IWriterConfiguration _config;

        public TimeoutModule(StandardKernel kernel)
            : base(kernel)
        {
        }

        public override void Start()
        {
            base.Start();

            _config = Kernel.Get<IWriterConfiguration>();
            _queue = Kernel.Get<IGlobalQueue>().DbTimeoutQueue;
            _db = Kernel.Get<IDbModule>();

            _deleteTimeout = TimeSpan.FromMilliseconds(_config.Restore.TimeoutDelete.DeleteTimeoutMls);

            if (_config.Restore.TimeoutDelete.ForceStart)
                AsyncTaskModule.AddAsyncTask(
                    new AsyncDataPeriod(_config.Restore.TimeoutDelete.PeriodRetryMls, PeriodMessage,
                        AsyncTasksNames.TimeoutDelete, -1), true);
        }

        public void Enable(bool forceStart = false)
        {
            AsyncTaskModule.DeleteTask(AsyncTasksNames.TimeoutDelete);
            AsyncTaskModule.AddAsyncTask(
                new AsyncDataPeriod(_config.Restore.TimeoutDelete.PeriodRetryMls, PeriodMessage,
                    AsyncTasksNames.TimeoutDelete, -1), forceStart);
        }

        public void Disable()
        {
            AsyncTaskModule.DeleteTask(AsyncTasksNames.TimeoutDelete);
        }

        public void StartDelete()
        {
            Enable(true);
        }

        public void RunDelete()
        {
            RunDeleteInner();
        }

        private void PeriodMessage(AsyncData obj)
        {
            RunDeleteInner();
        }

        private void RunDeleteInner()
        {
            if (_reader == null)
            {
                _reader = new TimeoutReaderFull(Kernel, IsMine, Process,
                    _config.Restore.TimeoutDelete.PackageSizeTimeout, _db, _queue);
                _reader.Start();
            }
            else if (_reader.IsComplete)
            {
                _reader.Dispose();
                _reader = new TimeoutReaderFull(Kernel, IsMine, Process,
                    _config.Restore.TimeoutDelete.PackageSizeTimeout, _db, _queue);
                _reader.Start();
            }
        }

        private void Process(InnerData data)
        {            
            _db.DeleteFull(data);
            PerfCounters.WriterCounters.Instance.DeleteTimeoutPerSec.OperationFinished();
        }

        private bool IsMine(MetaData data)
        {
            if (!data.DeleteTime.HasValue)
                return true;
            return DateTime.Now - data.DeleteTime >= _deleteTimeout;
        }

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                AsyncTaskModule.StopTask(AsyncTasksNames.TimeoutDelete);
            }

            base.Dispose(isUserCall);
        }
    }
}

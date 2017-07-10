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
        private readonly RestoreModuleConfiguration _configuration;
        private TimeoutReaderFull _reader;
        private readonly QueueConfiguration _queueConfiguration;
        private IDbModule _db;
        private QueueWithParam<InnerData> _queue;
        private readonly TimeSpan _deleteTimeout;

        public TimeoutModule(StandardKernel kernel, QueueConfiguration queueConfiguration,
            RestoreModuleConfiguration configuration)
            : base(kernel)
        {
            _configuration = configuration;
            _queueConfiguration = queueConfiguration;
            _deleteTimeout = configuration.DeleteTimeout;
        }

        public override void Start()
        {
            base.Start();

            _queue = Kernel.Get<IGlobalQueue>().DbTimeoutQueue;
            _db = Kernel.Get<IDbModule>();

            if (_configuration.IsForceStart)
                AsyncTaskModule.AddAsyncTask(
                    new AsyncDataPeriod(_configuration.PeriodRetry, PeriodMessage,
                        AsyncTasksNames.TimeoutDelete, -1), _configuration.IsForceStart);
        }

        public void Enable(bool forceStart = false)
        {
            AsyncTaskModule.DeleteTask(AsyncTasksNames.TimeoutDelete);
            AsyncTaskModule.AddAsyncTask(
                            new AsyncDataPeriod(_configuration.PeriodRetry, PeriodMessage,
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
                _reader = new TimeoutReaderFull(Kernel, IsMine, Process, _queueConfiguration, _db, _queue);
                _reader.Start();
            }
            else if (_reader.IsComplete)
            {
                _reader.Dispose();
                _reader = new TimeoutReaderFull(Kernel, IsMine, Process, _queueConfiguration, _db, _queue);
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

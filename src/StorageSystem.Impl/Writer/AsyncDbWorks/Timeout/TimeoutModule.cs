using System;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.Writer.Db;
using Qoollo.Impl.Writer.WriterNet;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Timeout
{
    internal class TimeoutModule:CommonAsyncWorkModule
    {
        private readonly RestoreModuleConfiguration _configuration;
        private TimeoutReaderFull _reader;
        private readonly QueueConfiguration _queueConfiguration;
        private readonly DbModuleCollection _db;
        private readonly QueueWithParam<InnerData> _queue;
        private readonly TimeSpan _deleteTimeout;

        public TimeoutModule(WriterNetModule net, AsyncTaskModule asyncTaskModule,
            QueueConfiguration queueConfiguration, DbModuleCollection db,
            RestoreModuleConfiguration configuration)
            : base(net, asyncTaskModule)
        {
            _configuration = configuration;
            _queueConfiguration = queueConfiguration;
            _db = db;
            _queue = GlobalQueue.Queue.DbTimeoutQueue;
            _deleteTimeout = configuration.DeleteTimeout;

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
                _reader = new TimeoutReaderFull(IsMine, Process, _queueConfiguration, _db, _queue);
                _reader.Start();
            }
            else if (_reader.IsComplete)
            {
                _reader.Dispose();
                _reader = new TimeoutReaderFull(IsMine, Process, _queueConfiguration, _db, _queue);
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

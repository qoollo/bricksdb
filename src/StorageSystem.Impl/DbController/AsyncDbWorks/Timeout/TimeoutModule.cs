using System;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DbController.Db;
using Qoollo.Impl.DbController.DbControllerNet;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Modules.Queue;

namespace Qoollo.Impl.DbController.AsyncDbWorks.Timeout
{
    internal class TimeoutModule:CommonAsyncWorkModule
    {
        private TimeoutReaderFull _reader;
        private QueueConfiguration _queueConfiguration;
        private DbModuleCollection _db;
        private QueueWithParam<InnerData> _queue;
        private TimeSpan _deleteTimeout;

        public TimeoutModule(DbControllerNetModule net, AsyncTaskModule asyncTaskModule,
            QueueConfiguration queueConfiguration, DbModuleCollection db,
            RestoreModuleConfiguration configuration)
            : base(net, asyncTaskModule)
        {
            _queueConfiguration = queueConfiguration;
            _db = db;
            _queue = GlobalQueue.Queue.DbTimeoutQueue;
            _deleteTimeout = configuration.DeleteTimeout;

            _asyncTaskModule.AddAsyncTask(
                new AsyncDataPeriod(configuration.PeriodRetry, PeriodMessage,
                    AsyncTasksNames.TimeoutDelete, -1), configuration.IsForceStart);            
        }

        private void PeriodMessage(AsyncData obj)
        {
            if (_reader == null)
            {            
                _reader = new TimeoutReaderFull(IsMine, Process, _queueConfiguration, _db, true,
                    _queue);

                _reader.Start();
            }
            else if(_reader.IsComplete)
            {
                _reader.Dispose();
                _reader = new TimeoutReaderFull(IsMine, Process, _queueConfiguration, _db, true,
                    _queue);

                _reader.Start();
            }
        }

        private void Process(InnerData data)
        {            
            _db.DeleteFull(data);
        }

        private bool IsMine(MetaData data)
        {            
            return DateTime.Now - data.DeleteTime >= _deleteTimeout;
        }

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                _asyncTaskModule.StopTask(AsyncTasksNames.TimeoutDelete);
            }

            base.Dispose(isUserCall);
        }
    }
}

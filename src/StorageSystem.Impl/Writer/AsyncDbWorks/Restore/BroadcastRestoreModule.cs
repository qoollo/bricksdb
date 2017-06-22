using System;
using System.Globalization;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Writer.AsyncDbWorks.Processes;
using Qoollo.Impl.Writer.Db;
using Qoollo.Impl.Writer.WriterNet;

namespace Qoollo.Impl.Writer.AsyncDbWorks.Restore
{
    internal class BroadcastRestoreModule: CommonAsyncWorkModule
    {
        private readonly WriterModel _writerModel;
        private readonly RestoreModuleConfiguration _configuration;
        private readonly DbModuleCollection _db;
        private readonly QueueConfiguration _queueConfiguration;
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

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

        public BroadcastRestoreModule(
            WriterModel writerModel,
            RestoreModuleConfiguration configuration,
            WriterNetModule writerNet,
            AsyncTaskModule asyncTaskModule,
            DbModuleCollection db,
            QueueConfiguration queueConfiguration)
            : base(writerNet, asyncTaskModule)
        {
            _writerModel = writerModel;
            _configuration = configuration;
            _db = db;
            _queueConfiguration = queueConfiguration;
        }

        private string _lastDateTime;

        public void RestoreIncome(bool isSystemUpdated)
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

            //_restore = new SingleServerRestoreProcess(_db, _writerModel, WriterNet,
            //    tableName, _remoteServer, isSystemUpdated, _queueConfiguration);
            //_restore.Start();
        }
    }
}
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.System.DbController;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DbController.AsyncDbWorks.Readers;
using Qoollo.Impl.DbController.Db;
using Qoollo.Impl.DbController.DbControllerNet;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Modules.Queue;

namespace Qoollo.Impl.DbController.AsyncDbWorks.Restore
{
    internal class TransferRestoreModule:CommonAsyncWorkModule
    {
        private RestoreModuleConfiguration _configuration;
        private DbModuleCollection _db;
        private ServerId _local;
        private ServerId _remote;        
        private ReaderFullBase _reader;
        private QueueConfiguration _queueConfiguration;
        private List<KeyValuePair<string, string>> _hash;
        private GlobalQueueInner _queue;

        private ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        public TransferRestoreModule(RestoreModuleConfiguration configuration, DbControllerNetModule dbControllerNet,
                                     AsyncTaskModule asyncTaskModule, DbModuleCollection db, ServerId local,
                                     QueueConfiguration queueConfiguration)
            : base(dbControllerNet, asyncTaskModule)
        {
            Contract.Requires(configuration != null);
            Contract.Requires(db != null);
            Contract.Requires(local != null);
            Contract.Requires(queueConfiguration != null);

            _queue = GlobalQueue.Queue;
            _db = db;
            _configuration = configuration;
            _local = local;
            _queueConfiguration = queueConfiguration;
        }

        public void RestoreIncome(ServerId server, bool isSystemUpdated, List<KeyValuePair<string, string>> hash,
            string tableName)
        {
            Logger.Logger.Instance.Debug(string.Format("transafer start {0}, {1}", server, hash), "restore");
            _lock.EnterReadLock();
            bool exit = _isStart;
            _lock.ExitReadLock();
            if (exit)
                return;

            _lock.EnterWriteLock();
            _isStart = true;
            _lock.ExitWriteLock();

            _remote = server;

            _hash = hash;

            _asyncTaskModule.AddAsyncTask(
                new AsyncDataPeriod(_configuration.PeriodRetry, RestoreAnswerCallback, AsyncTasksNames.RestoreLocal,
                    -1), false);

            _reader = new RestoreReaderFull(IsMine, ProcessData, _queueConfiguration, _db, isSystemUpdated,
                tableName,_queue.DbRestoreQueue);
            _reader.Start();
        }

        private void ProcessData(InnerData data)
        {
            _lock.EnterReadLock();
            bool exit = _isStart;
            _lock.ExitReadLock();
            if (!exit)
                return;

            data.Transaction.OperationName = OperationName.RestoreUpdate;
            data.Transaction.OperationType = OperationType.Async;

            var result = DbControllerNet.ProcessSync(_remote, data);

            if (result is FailNetResult)
            {
                Logger.Logger.Instance.InfoFormat("Servers {0} unavailable in recover process", _remote);
                _asyncTaskModule.DeleteTask(AsyncTasksNames.RestoreLocal);
                _reader.Stop();

                _lock.EnterWriteLock();
                _isStart = false;
                _lock.ExitWriteLock();
            }
            else if(!_local.Equals(_remote))
            {
                _db.Delete(data);
            }
        }

        public bool IsMine(MetaData data)
        {
            return
                _hash.Exists(
                    x =>
                    HashComparer.Compare(x.Key, data.Hash) <= 0 &&
                    HashComparer.Compare(data.Hash, x.Value) <= 0);
        }

        private void RestoreAnswerCallback(AsyncData obj)
        {
            Logger.Logger.Instance.Debug(
                string.Format("Async complete = {0}, start = {1}", _reader.IsComplete, _isStart), "restore");

            if (_reader.IsComplete && _isStart)
            {
                _asyncTaskModule.DeleteTask(AsyncTasksNames.RestoreLocal);
                _lock.EnterWriteLock();
                _isStart = false;
                _lock.ExitWriteLock();
                DbControllerNet.SendToController(_remote, new RestoreCompleteCommand(_local));
                _reader.Dispose();
            }
            else
            {
                if(_reader.IsQueueEmpty && _isStart)
                    _reader.GetAnotherData();

                DbControllerNet.SendToController(_remote, new RestoreInProcessCommand(_local));
            }
        }

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                _asyncTaskModule.StopTask(AsyncTasksNames.RestoreLocal);
                if(_reader!=null)
                    _reader.Dispose();
            }

            base.Dispose(isUserCall);
        }
    }
}

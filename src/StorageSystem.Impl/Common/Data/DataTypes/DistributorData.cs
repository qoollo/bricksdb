using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Common.Data.DataTypes
{
    public class DistributorData
    {
        public class DistributorLock : IDisposable
        {
            private readonly DistributorData _data;

            public DistributorLock(DistributorData data)
            {
                _data = data;
                _data.Lock();
            }

            public void Dispose()
            {
                _data.Unlock();
            }
        }

        public bool IsSyncAnswerSended { get; private set; }
        public bool IsRollbackSended { get; private set; }
        public int TransactionAnswersCount { get; private set; }

        /// <summary>
        /// Servers for where data store
        /// </summary>        
        public List<ServerId> Destination { get; set; }        

        public DistributorData()
        {
            _lock = new ReaderWriterLockSlim();
            IsSyncAnswerSended = false;
            IsRollbackSended = false;
            TransactionAnswersCount = 0;
        }

        private readonly ReaderWriterLockSlim _lock;        

        private void Lock()
        {
            _lock.EnterWriteLock();
        }

        private void Unlock()
        {
            _lock.ExitWriteLock();
        }

        public DistributorLock GetLock()
        {
            return new DistributorLock(this);
        }

        public void SendSyncAnswer()
        {
            IsRollbackSended = true;
        }

        public void SendRollback()
        {
            IsRollbackSended = true;
        }

        public void IncreaseTransactionAnswersCount()
        {
            TransactionAnswersCount++;
        }
    }
}

using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Configurations;

namespace Qoollo.Impl.Modules.Queue
{
    internal static class GlobalQueue
    {
        private static GlobalQueueInner _queue = new GlobalQueueInner();

        public static GlobalQueueInner Queue { get { return _queue; } }

        public static void SetQueue(GlobalQueueInner queue)
        {
            _queue = queue;
        }
    }

    internal class GlobalQueueInner:ControlModule
    {
        private QueueWithParam<InnerData> _proxyInputWriteAndUpdateQueue;
        public QueueWithParam<InnerData> ProxyInputWriteAndUpdateQueue { get { return _proxyInputWriteAndUpdateQueue; } }

        private QueueWithParam<InnerData> _proxyInputOtherQueue;
        public QueueWithParam<InnerData> ProxyInputOtherQueue { get { return _proxyInputOtherQueue; } }

        private QueueWithParam<NetCommand> _proxyDistributorQueue;
        public QueueWithParam<NetCommand> ProxyDistributorQueue { get { return _proxyDistributorQueue; } }

        private QueueWithParam<NetCommand> _distributorDistributorQueue;
        public QueueWithParam<NetCommand> DistributorDistributorQueue { get { return _distributorDistributorQueue; } }

        private QueueWithParam<InnerData> _dbInputRollbackQueue;
        public QueueWithParam<InnerData> DbInputRollbackQueue { get { return _dbInputRollbackQueue; } }

        private QueueWithParam<InnerData> _dbInputProcessQueue;
        public QueueWithParam<InnerData> DbInputProcessQueue { get { return _dbInputProcessQueue; } }

        private QueueWithParam<NetCommand> _dbDistributorInnerQueue;
        public QueueWithParam<NetCommand> DbDistributorInnerQueue { get { return _dbDistributorInnerQueue; } }

        private QueueWithParam<NetCommand> _dbDistributorOuterQueue;
        public QueueWithParam<NetCommand> DbDistributorOuterQueue { get { return _dbDistributorOuterQueue; } }

        private QueueWithParam<InnerData> _dbRestoreQueue;
        public QueueWithParam<InnerData> DbRestoreQueue { get { return _dbRestoreQueue; } }

        private QueueWithParam<Transaction> _transactionQueue;
        public QueueWithParam<Transaction> TransactionQueue { get { return _transactionQueue; } }

        private QueueWithParam<Transaction> _transactionAnswerQueue;
        public QueueWithParam<Transaction> TransactionAnswerQueue { get { return _transactionAnswerQueue; } }

        private QueueWithParam<Transaction> _distributorTransactionCallbackQueue;
        public QueueWithParam<Transaction> DistributorTransactionCallbackQueue { get { return _distributorTransactionCallbackQueue; } }

        private QueueWithParam<InnerData> _distributorReadQueue;
        public QueueWithParam<InnerData> DistributorReadQueue { get { return _distributorReadQueue; } }

        private QueueWithParam<InnerData> _dbTimeoutQueue;
        public QueueWithParam<InnerData> DbTimeoutQueue { get { return _dbTimeoutQueue; } }

        public GlobalQueueInner()
        {
            _proxyInputWriteAndUpdateQueue = new QueueWithParam<InnerData>();
            _proxyInputOtherQueue = new QueueWithParam<InnerData>();
            _proxyDistributorQueue = new QueueWithParam<NetCommand>();
            _distributorDistributorQueue = new QueueWithParam<NetCommand>();
            _dbInputRollbackQueue = new QueueWithParam<InnerData>();
            _dbInputProcessQueue = new QueueWithParam<InnerData>();
            _dbDistributorInnerQueue = new QueueWithParam<NetCommand>();
            _dbDistributorOuterQueue = new QueueWithParam<NetCommand>();
            _dbRestoreQueue = new QueueWithParam<InnerData>();
            _transactionQueue = new QueueWithParam<Transaction>();
            _transactionAnswerQueue = new QueueWithParam<Transaction>();
            _distributorTransactionCallbackQueue = new QueueWithParam<Transaction>();
            _distributorReadQueue = new QueueWithParam<InnerData>();
            _dbTimeoutQueue = new QueueWithParam<InnerData>();
        }

        public override void Start()
        {
            //TODO не трогать!
            _transactionQueue.SetConfiguration(new QueueConfiguration(1, 10000));

            _proxyInputWriteAndUpdateQueue.Start();
            _proxyInputOtherQueue.Start();
            _proxyDistributorQueue.Start();
            _distributorDistributorQueue.Start();
            _dbInputRollbackQueue.Start();
            _dbInputProcessQueue.Start();
            _dbDistributorInnerQueue.Start();
            _dbDistributorOuterQueue.Start();
            _dbRestoreQueue.Start();
            _transactionQueue.Start();
            _transactionAnswerQueue.Start();
            _distributorTransactionCallbackQueue.Start();
            _distributorReadQueue.Start();
            _dbTimeoutQueue.Start();
        }

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                _proxyInputWriteAndUpdateQueue.Dispose();
                _proxyInputOtherQueue.Dispose();
                _proxyDistributorQueue.Dispose();
                _distributorDistributorQueue.Dispose();
                _dbInputRollbackQueue.Dispose();
                _dbInputProcessQueue.Dispose();
                _dbDistributorInnerQueue.Dispose();
                _dbDistributorOuterQueue.Dispose();
                _dbRestoreQueue.Dispose();
                _transactionQueue.Dispose();
                _transactionAnswerQueue.Dispose();
                _distributorTransactionCallbackQueue.Dispose();
                _distributorReadQueue.Dispose();
                _dbTimeoutQueue.Dispose();
            }

            base.Dispose(isUserCall);
        }
    }
}

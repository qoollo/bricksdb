using System.Collections.Generic;
using Ninject;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Configurations.Queue;

namespace Qoollo.Impl.Modules.Queue
{
    internal class GlobalQueue : ControlModule, IGlobalQueue
    {
        private readonly QueueWithParam<InnerData> _proxyInputWriteAndUpdateQueue;
        public QueueWithParam<InnerData> ProxyInputWriteAndUpdateQueue { get { return _proxyInputWriteAndUpdateQueue; } }

        private readonly QueueWithParam<InnerData> _proxyInputOtherQueue;
        public QueueWithParam<InnerData> ProxyInputOtherQueue { get { return _proxyInputOtherQueue; } }

        private readonly QueueWithParam<NetCommand> _proxyDistributorQueue;
        public QueueWithParam<NetCommand> ProxyDistributorQueue { get { return _proxyDistributorQueue; } }

        private readonly QueueWithParam<NetCommand> _distributorDistributorQueue;
        public QueueWithParam<NetCommand> DistributorDistributorQueue { get { return _distributorDistributorQueue; } }

        private readonly QueueWithParam<InnerData> _dbInputRollbackQueue;
        public QueueWithParam<InnerData> DbInputRollbackQueue { get { return _dbInputRollbackQueue; } }

        private readonly QueueWithParam<InnerData> _dbInputProcessQueue;
        public QueueWithParam<InnerData> DbInputProcessQueue { get { return _dbInputProcessQueue; } }

        private readonly QueueWithParam<NetCommand> _dbDistributorInnerQueue;
        public QueueWithParam<NetCommand> DbDistributorInnerQueue { get { return _dbDistributorInnerQueue; } }        

        private readonly QueueWithParam<InnerData> _dbRestoreQueue;
        public QueueWithParam<InnerData> DbRestoreQueue { get { return _dbRestoreQueue; } }

        private readonly QueueWithParam<List<InnerData>> _dbRestorePackageQueue;
        public QueueWithParam<List<InnerData>> DbRestorePackageQueue { get { return _dbRestorePackageQueue; } }

        private readonly QueueWithParam<Transaction> _transactionQueue;
        public QueueWithParam<Transaction> TransactionQueue { get { return _transactionQueue; } }

        private readonly QueueWithParam<Transaction> _transactionAnswerQueue;
        public QueueWithParam<Transaction> TransactionAnswerQueue { get { return _transactionAnswerQueue; } }

        private readonly QueueWithParam<Transaction> _distributorTransactionCallbackQueue;
        public QueueWithParam<Transaction> DistributorTransactionCallbackQueue { get { return _distributorTransactionCallbackQueue; } }        

        private readonly QueueWithParam<InnerData> _dbTimeoutQueue;
        public QueueWithParam<InnerData> DbTimeoutQueue { get { return _dbTimeoutQueue; } }

        public string Name { get; }

        public GlobalQueue(StandardKernel kernel, string name = ""):base(kernel)
        {
            Name = name;

            var configuration = kernel.Get<IQueueConfiguration>();

            _proxyInputWriteAndUpdateQueue = new QueueWithParam<InnerData>("proxyInputWriteAndUpdateQueue", configuration.ProxyInput);
            _proxyInputOtherQueue = new QueueWithParam<InnerData>("proxyInputOtherQueue", configuration.ProxyInputOther);
            _proxyDistributorQueue = new QueueWithParam<NetCommand>("proxyDistributorQueue", configuration.ProxyDistributor);

            _distributorDistributorQueue = new QueueWithParam<NetCommand>("distributorDistributorQueue", configuration.DistributorDistributor);
            _transactionQueue = new QueueWithParam<Transaction>("transactionQueue", configuration.DistributorTransaction);
            _distributorTransactionCallbackQueue = new QueueWithParam<Transaction>("distributorTransactionCallbackQueue", configuration.DistributorTransactionCallback);

            _dbInputRollbackQueue = new QueueWithParam<InnerData>("dbInputRollbackQueue", configuration.WriterInputRollback);
            _dbInputProcessQueue = new QueueWithParam<InnerData>("dbInputProcessQueue", configuration.WriterInput);
            _dbDistributorInnerQueue = new QueueWithParam<NetCommand>("dbDistributorInnerQueue", configuration.WriterDistributor);
            _dbRestoreQueue = new QueueWithParam<InnerData>("dbRestoreQueue", configuration.WriterRestore);
            _dbRestorePackageQueue = new QueueWithParam<List<InnerData>>("dbRestorePackageQueue", configuration.WriterRestorePackage);
            _transactionAnswerQueue = new QueueWithParam<Transaction>("transactionAnswerQueue", configuration.WriterTransactionAnswer);
            _dbTimeoutQueue = new QueueWithParam<InnerData>("dbTimeoutQueue", configuration.WriterTimeout);
        }

        public override void Start()
        {
            _proxyInputWriteAndUpdateQueue.Start();
            _proxyInputOtherQueue.Start();
            _proxyDistributorQueue.Start();
            _distributorDistributorQueue.Start();
            _dbInputRollbackQueue.Start();
            _dbInputProcessQueue.Start();
            _dbDistributorInnerQueue.Start();
            _dbRestoreQueue.Start();
            _dbRestorePackageQueue.Start();
            _transactionQueue.Start();
            _transactionAnswerQueue.Start();
            _distributorTransactionCallbackQueue.Start();
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
                _dbRestoreQueue.Dispose();
                _dbRestorePackageQueue.Dispose();
                _transactionQueue.Dispose();
                _transactionAnswerQueue.Dispose();
                _distributorTransactionCallbackQueue.Dispose();
                _dbTimeoutQueue.Dispose();
            }

            base.Dispose(isUserCall);
        }
    }
}

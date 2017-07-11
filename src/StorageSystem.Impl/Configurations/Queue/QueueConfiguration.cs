namespace Qoollo.Impl.Configurations.Queue
{
    public class SingleQueueConfiguration
    {
        public int CountThreads { get; protected set; }
        public int MaxSize { get; protected set; }
    }

    public class QueueConfiguration : IQueueConfiguration
    {
        public SingleQueueConfiguration WriterDistributor { get; protected set; }
        public SingleQueueConfiguration WriterInput { get; protected set; }
        public SingleQueueConfiguration WriterInputRollback { get; protected set; }
        public SingleQueueConfiguration WriterRestore { get; protected set; }
        public SingleQueueConfiguration WriterRestorePackage { get; protected set; }
        public SingleQueueConfiguration WriterTimeout { get; protected set; }
        public SingleQueueConfiguration WriterTransactionAnswer { get; protected set; }

        public SingleQueueConfiguration DistributorDistributor { get; protected set; }
        public SingleQueueConfiguration DistributorTransaction { get; protected set; }
        public SingleQueueConfiguration DistributorTransactionCallback { get; protected set; }

        public SingleQueueConfiguration ProxyDistributor { get; protected set; }
        public SingleQueueConfiguration ProxyInput { get; protected set; }
        public SingleQueueConfiguration ProxyInputOther { get; protected set; }
    }
}
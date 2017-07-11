namespace Qoollo.Impl.Configurations.Queue
{
    public interface IQueueConfiguration
    {
        SingleQueueConfiguration DistributorDistributor { get; }
        SingleQueueConfiguration DistributorTransaction { get; }
        SingleQueueConfiguration DistributorTransactionCallback { get; }
        SingleQueueConfiguration ProxyDistributor { get; }
        SingleQueueConfiguration ProxyInput { get; }
        SingleQueueConfiguration ProxyInputOther { get; }
        SingleQueueConfiguration WriterDistributor { get; }
        SingleQueueConfiguration WriterInput { get; }
        SingleQueueConfiguration WriterInputRollback { get; }
        SingleQueueConfiguration WriterRestore { get; }
        SingleQueueConfiguration WriterRestorePackage { get; }
        SingleQueueConfiguration WriterTimeout { get; }
        SingleQueueConfiguration WriterTransactionAnswer { get; }
    }
}
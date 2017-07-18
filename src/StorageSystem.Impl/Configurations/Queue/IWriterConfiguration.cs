namespace Qoollo.Impl.Configurations.Queue
{
    public interface IWriterConfiguration
    {
        int PackageSizeRestore { get; }
        int PackageSizeTimeout { get; }

        NetConfiguration NetDistributor { get; }
        NetConfiguration NetCollector { get; }
    }

    public class WriterConfiguration : IWriterConfiguration
    {
        public int PackageSizeRestore { get; protected set; }
        public int PackageSizeTimeout { get; protected set; }
        public NetConfiguration NetDistributor { get; protected set; }
        public NetConfiguration NetCollector { get; protected set; }
    }
}
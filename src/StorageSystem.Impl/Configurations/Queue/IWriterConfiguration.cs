namespace Qoollo.Impl.Configurations.Queue
{
    public interface IWriterConfiguration
    {
        int PackageSizeRestore { get; }
        int PackageSizeTimeout { get; }
    }

    public class WriterConfiguration : IWriterConfiguration
    {
        public int PackageSizeRestore { get; protected set; }
        public int PackageSizeTimeout { get; protected set; }
    }
}
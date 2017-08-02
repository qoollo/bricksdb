namespace Qoollo.Impl.Configurations.Queue
{
    public interface IWriterConfiguration
    {
        int PackageSizeRestore { get; }
        int PackageSizeTimeout { get; }

        NetConfiguration NetDistributor { get; }
        NetConfiguration NetCollector { get; }
        TimeoutsConfiguration Timeouts { get; }
        RestoreConfiguration Restore { get; }
    }

    public class WriterConfiguration : IWriterConfiguration
    {
        public int PackageSizeRestore { get; protected set; }
        public int PackageSizeTimeout { get; protected set; }
        public NetConfiguration NetDistributor { get; protected set; }
        public NetConfiguration NetCollector { get; protected set; }
        public TimeoutsConfiguration Timeouts { get; protected set; }
        public RestoreConfiguration Restore { get; protected set; }
    }

    public class TimeoutsConfiguration
    {
        public TimeoutConfiguration ServersPingMls { get; protected set; }
    }

    public class RestoreConfiguration
    {
        public TimeoutDeleteConfiguration TimeoutDelete { get; protected set; }
        public RestoreInitConfiguration Initiator { get; protected set; }
    }

    public class TimeoutDeleteConfiguration
    {
        public int PeriodRetryMls { get; protected set; }
        public bool ForceStart { get; protected set; }
        public int DeleteTimeoutMls { get; protected set; }
    }

    public class RestoreInitConfiguration
    {
        public int PeriodRetryMls { get; protected set; }
        public int CountRetry { get; protected set; }
    }
}
namespace Qoollo.Impl.Configurations
{
    public interface IWriterConfiguration
    {
        int PackageSizeRestore { get; }

        string RestoreStateFilename { get; }

        NetConfiguration NetDistributor { get; }
        NetConfiguration NetCollector { get; }
        TimeoutsConfiguration Timeouts { get; }
        RestoreConfiguration Restore { get; }
    }

    public class WriterConfiguration : IWriterConfiguration
    {
        public int PackageSizeRestore { get; protected set; }
        public string RestoreStateFilename { get; protected set; }
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
        public RestoreBroadcastConfiguration Broadcast { get; protected set; }
        public RestoreTransferConfiguration Transfer { get; protected set; }
    }

    public class TimeoutDeleteConfiguration
    {
        public int PeriodRetryMls { get; protected set; }
        public bool ForceStart { get; protected set; }
        public int DeleteTimeoutMls { get; protected set; }
        public int PackageSizeTimeout { get; protected set; }
    }

    public class RestoreInitConfiguration
    {
        public int PeriodRetryMls { get; protected set; }
        public int CountRetry { get; protected set; }
    }

    public class RestoreBroadcastConfiguration
    {
        public int PeriodRetryMls { get; protected set; }
        public bool UsePackage { get; protected set; }
    }

    public class RestoreTransferConfiguration
    {
        public int PeriodRetryMls { get; protected set; }
        public bool UsePackage { get; protected set; }
    }
}
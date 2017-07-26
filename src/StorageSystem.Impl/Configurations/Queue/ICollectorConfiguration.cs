namespace Qoollo.Impl.Configurations.Queue
{
    public interface ICollectorConfiguration
    {
         Collector.TimeoutsConfiguration Timeouts { get; }
    }

    public class CollectorConfiguration : ICollectorConfiguration
    {
        public Collector.TimeoutsConfiguration Timeouts { get; protected set; }
    }

    namespace Collector
    {
        public class TimeoutsConfiguration
        { 
            public TimeoutConfiguration ServersPingMls { get; protected set; }
            public TimeoutConfiguration DistributorUpdateHashMls { get; protected set; }
        }
    }
}
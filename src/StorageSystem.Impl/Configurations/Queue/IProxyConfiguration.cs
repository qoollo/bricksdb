namespace Qoollo.Impl.Configurations.Queue
{
    public interface IProxyConfiguration
    {
        NetConfiguration NetDistributor { get; }
        Proxy.TimeoutsConfiguration Timeouts { get; }

    }

    public class ProxyConfiguration : IProxyConfiguration
    {
        public NetConfiguration NetDistributor { get; protected set; }
        public Proxy.TimeoutsConfiguration Timeouts { get; protected set; }
    }

    namespace Proxy
    {
        public class TimeoutsConfiguration
        {
            public TimeoutConfiguration ServersPingMls { get; protected set; }
            public TimeoutConfiguration DistributorUpdateInfoMls { get; protected set; }
        }
    }
}
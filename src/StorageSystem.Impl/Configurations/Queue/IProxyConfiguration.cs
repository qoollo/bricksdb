namespace Qoollo.Impl.Configurations.Queue
{
    public interface IProxyConfiguration
    {
        NetConfiguration NetDistributor { get; }
        Proxy.TimeoutsConfiguration Timeouts { get; }
        ProxyCacheConfiguration Cache { get; }
    }

    public class ProxyConfiguration : IProxyConfiguration
    {
        public NetConfiguration NetDistributor { get; protected set; }
        public Proxy.TimeoutsConfiguration Timeouts { get; protected set; }
        public ProxyCacheConfiguration Cache { get; protected set; }
    }

    public class ProxyCacheConfiguration
    {
        public int Transaction { get; protected set; }
        public int Support { get; protected set; }

        public ProxyCacheConfiguration(int transaction, int support)
        {
            Transaction = transaction;
            Support = support;
        }

        public ProxyCacheConfiguration()
        {
        }
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
namespace Qoollo.Impl.Configurations.Queue
{
    public interface IProxyConfiguration
    {
        NetConfiguration NetDistributor { get; }

    }

    public class ProxyConfiguration : IProxyConfiguration
    {
        public NetConfiguration NetDistributor { get; protected set; }
    }
}
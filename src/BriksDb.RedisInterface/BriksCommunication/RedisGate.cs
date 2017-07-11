using Qoollo.Client.Configuration;
using Qoollo.Client.ProxyGate;

namespace BricksDb.RedisInterface.BriksCommunication
{
    class RedisGate: ProxyApi
    {
        private IStorage<string, string> _data;

    public RedisGate(NetConfiguration netConfiguration, ProxyConfiguration proxyConfiguration,
        TimeoutConfiguration timeoutConfiguration)
        : base(netConfiguration, proxyConfiguration, timeoutConfiguration)
    {
    }

    public RedisGate(NetConfiguration netConfiguration, ProxyConfiguration proxyConfiguration)
        : base(netConfiguration, proxyConfiguration)
    {
    }

    public IStorage<string, string> RedisTable
    {
        get { return _data; }
    }

    protected override void InnerBuild()
    {
        _data = RegistrateApi("RedisTable", false, new RedisDataProvider());            
    }
    }
}

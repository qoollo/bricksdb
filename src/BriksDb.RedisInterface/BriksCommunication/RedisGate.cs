using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Client.Configuration;
using Qoollo.Client.ProxyGate;

namespace BricksDb.RedisInterface.BriksCommunication
{
    class RedisGate: ProxyApi
    {
        private IStorage<string, string> _data;

    public RedisGate(NetConfiguration netConfiguration, ProxyConfiguration proxyConfiguration,
        CommonConfiguration commonConfiguration, TimeoutConfiguration timeoutConfiguration)
        : base(netConfiguration, proxyConfiguration, commonConfiguration, timeoutConfiguration)
    {
    }

    public RedisGate(NetConfiguration netConfiguration, ProxyConfiguration proxyConfiguration,
        CommonConfiguration commonConfiguration)
        : base(netConfiguration, proxyConfiguration, commonConfiguration)
    {
    }

    public IStorage<string, string> DataWithBufferTable
    {
        get { return _data; }
    }

    protected override void InnerBuild()
    {
        _data = RegistrateApi("RedisTable", false, new RedisDataProvider());            
    }
    }
}

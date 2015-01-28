using System;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Proxy;
using Qoollo.Impl.Proxy.Caches;
using Qoollo.Impl.Proxy.ProxyNet;

namespace Qoollo.Tests.TestProxy
{
    class TestProxyDistributorModule : ProxyDistributorModule
    {
        public TestProxyDistributorModule(AsyncProxyCache asyncProxyCache, ProxyNetModule net,
            QueueConfiguration queueConfiguration, ServerId local, AsyncTasksConfiguration asyncGetData,
            AsyncTasksConfiguration asyncPing)
            : base(asyncProxyCache, net, queueConfiguration, local, asyncGetData, asyncPing)
        {
        }

        public TestProxyDistributorModule() :
            this(new AsyncProxyCache(TimeSpan.FromDays(1)), null, new QueueConfiguration(1, 1), null, null, null)
        {
        }

        public override void ServerNotAvailable(ServerId server)
        {
        }

    }
}

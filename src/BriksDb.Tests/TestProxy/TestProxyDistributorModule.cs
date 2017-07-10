using System;
using Ninject;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Proxy;

namespace Qoollo.Tests.TestProxy
{
    class TestProxyDistributorModule : ProxyDistributorModule
    {
        public TestProxyDistributorModule(StandardKernel kernel, 
            QueueConfiguration queueConfiguration, ServerId local, AsyncTasksConfiguration asyncGetData,
            AsyncTasksConfiguration asyncPing)
            : base(kernel, queueConfiguration, local, asyncGetData, asyncPing)
        {
        }

        public TestProxyDistributorModule(StandardKernel kernel) :
            this(kernel, new QueueConfiguration(1, 1), null, null, null)
        {
        }

        public override void ServerNotAvailable(ServerId server)
        {
        }

    }
}

using System;
using Ninject;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Proxy;

namespace Qoollo.Tests.TestProxy
{
    class TestProxyDistributorModule : ProxyDistributorModule
    {
        public TestProxyDistributorModule(StandardKernel kernel)
            : base(kernel)
        {
        }

        public override void ServerNotAvailable(ServerId server)
        {
        }

    }
}

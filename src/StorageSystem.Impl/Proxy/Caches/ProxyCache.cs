using System;
using Ninject;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Modules.Cache;

namespace Qoollo.Impl.Proxy.Caches
{
    internal class ProxyCache:CacheModule<ServerId>
    {
        public ProxyCache(StandardKernel kernel, TimeSpan timeout) : base(kernel, timeout)
        {
        }

        protected override void RemovedCallback(string key, ServerId obj)
        {
        }
    }
}

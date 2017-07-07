using System;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Modules.Cache;
using Qoollo.Impl.Proxy.Interfaces;

namespace Qoollo.Impl.Proxy.Caches
{
    internal class ProxyCache:CacheModule<ServerId>, IProxyCache
    {
        public ProxyCache(TimeSpan timeout) : base(timeout)
        {
        }

        protected override void RemovedCallback(string key, ServerId obj)
        {
        }
    }
}

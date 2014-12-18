using System;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Modules.Cache;

namespace Qoollo.Impl.Proxy.Caches
{
    internal class ProxyCache:CacheModule<ServerId>
    {
        public ProxyCache(TimeSpan timeout) : base(timeout)
        {
        }

        protected override void RemovedCallback(string key, ServerId obj)
        {
        }
    }
}

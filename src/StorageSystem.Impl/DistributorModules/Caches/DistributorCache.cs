using System;
using Ninject;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Modules.Cache;

namespace Qoollo.Impl.DistributorModules.Caches
{
    internal class DistributorCache:CacheModule<Common.Data.TransactionTypes.Transaction>
    {
        private readonly TimeSpan _aliveTimeout;

        public DistributorCache(StandardKernel kernel, TimeSpan timeout, TimeSpan aliveTimeout) 
            : base(kernel, timeout)
        {
            _aliveTimeout = aliveTimeout;
        }

        protected override void RemovedCallback(string key, Common.Data.TransactionTypes.Transaction obj)
        {
            if (obj.State == TransactionState.Complete)
                this.AddAliveToCache(key, obj, _aliveTimeout);
        }

        public void Update(string key, Common.Data.TransactionTypes.Transaction obj)
        {
            Remove(key);
            AddAliveToCache(key, obj, _aliveTimeout);
        }

    }
}

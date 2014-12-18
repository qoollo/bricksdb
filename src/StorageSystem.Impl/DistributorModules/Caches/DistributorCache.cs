using System;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Modules.Cache;

namespace Qoollo.Impl.DistributorModules.Caches
{
    internal class DistributorCache:CacheModule<Common.Data.TransactionTypes.Transaction>
    {
        private TimeSpan _aliveTimeout;

        public DistributorCache(TimeSpan timeout, TimeSpan aliveTimeout) : base(timeout)
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

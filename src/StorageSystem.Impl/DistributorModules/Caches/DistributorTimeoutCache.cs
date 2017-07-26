using System;
using System.Diagnostics.Contracts;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Configurations.Queue;
using Qoollo.Impl.DistributorModules.Interfaces;
using Qoollo.Impl.Modules.Cache;

namespace Qoollo.Impl.DistributorModules.Caches
{
    internal class DistributorTimeoutCache : CacheModule<InnerData>, IDistributorTimeoutCache
    {
        private readonly TimeSpan _aliveTimeout;

        public Action<InnerData> DataTimeout { get; set; }

        public DistributorTimeoutCache(DistributorCacheConfiguration cacheConfiguration)
            : base(TimeSpan.FromMilliseconds(cacheConfiguration.TimeAliveBeforeDeleteMls))
        {
            Contract.Requires(cacheConfiguration != null);
            _aliveTimeout = TimeSpan.FromMilliseconds(cacheConfiguration.TimeAliveAfterUpdateMls);
            DataTimeout = data => { };
        }

        protected override void RemovedCallback(string key, InnerData obj)
        {
            if (obj.Transaction.State == TransactionState.Complete)
                AddAliveToCache(key, obj, _aliveTimeout);

            else if (obj.Transaction.State == TransactionState.TransactionInProcess)
                DataTimeout(obj);         
        }

        public void Update(string key, InnerData obj)
        {
            Remove(key);
            AddAliveToCache(key, obj, _aliveTimeout);
        }

        public void AddDataToCache(InnerData data)
        {
            var item = new InnerData(data.Transaction)
            {
                Key = data.Key ,
                DistributorData = data.DistributorData
            };
            AddToCache(data.Transaction.CacheKey, item);
        }

        public void UpdateDataToCache(InnerData data)
        {
            var item = new InnerData(data.Transaction)
            {
                Key = data.Key ,
                DistributorData = data.DistributorData
            };
            Update(data.Transaction.CacheKey, item);
        }        
    }
}

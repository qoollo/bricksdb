using System;
using System.Diagnostics.Contracts;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Modules.Cache;

namespace Qoollo.Impl.DistributorModules.Caches
{
    internal class DistributorTimeoutCache : CacheModule<InnerData>
    {
        private readonly TimeSpan _aliveTimeout;
        private MainLogicModule _main;

        public DistributorTimeoutCache(TimeSpan timeout, TimeSpan aliveTimeout)
            : base(timeout)
        {
            Contract.Requires(aliveTimeout!=null);
            Contract.Requires(aliveTimeout.TotalMilliseconds>0);

            _aliveTimeout = aliveTimeout;
        }

        public void SetMainLogicModule(MainLogicModule main)
        {
            Contract.Requires(main!=null);
            _main = main;
        }

        protected override void RemovedCallback(string key, InnerData obj)
        {
            if (obj.Transaction.State == TransactionState.Complete)
                AddAliveToCache(key, obj, _aliveTimeout);

            else if (obj.Transaction.State == TransactionState.TransactionInProcess)
            {
                _main.DataTimeout(obj);
            }            
        }

        public void Update(string key, InnerData obj)
        {
            Remove(key);
            AddAliveToCache(key, obj, _aliveTimeout);
        }
    }
}

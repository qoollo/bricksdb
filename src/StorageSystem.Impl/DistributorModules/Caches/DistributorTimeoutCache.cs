using System;
using System.Diagnostics.Contracts;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Modules.Cache;

namespace Qoollo.Impl.DistributorModules.Caches
{
    internal class DistributorTimeoutCache : CacheModule<Common.Data.TransactionTypes.Transaction>
    {
        private TimeSpan _aliveTimeout;
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

        protected override void RemovedCallback(string key, Common.Data.TransactionTypes.Transaction obj)
        {
            if (obj.State == TransactionState.Complete)
                AddAliveToCache(key, obj, _aliveTimeout);

            else if (obj.State == TransactionState.TransactionInProcess)
            {
                _main.DataTimeout(obj);
            }            
        }

        public void Update(string key, Common.Data.TransactionTypes.Transaction obj)
        {
            Remove(key);
            AddAliveToCache(key, obj, _aliveTimeout);
        }
    }
}

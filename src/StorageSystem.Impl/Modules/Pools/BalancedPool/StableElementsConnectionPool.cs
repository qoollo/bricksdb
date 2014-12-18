using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.ServiceModel;
using System.Threading;
using Core.ServiceClasses.Pool;

namespace Qoollo.Impl.Modules.Pools.BalancedPool
{
    internal class StableConnectionElement<TApi> : StableConcurrentConnection<TApi>
    {
        public StableConnectionElement(ChannelFactory<TApi> factory, int maxAsyncQueryCount, bool syncFirstOpen = false)
            : base(factory, maxAsyncQueryCount, syncFirstOpen)
        {
        }
    }
    
    internal class StableConnectionElementComparer<TApi> : IComparer<StableConnectionElement<TApi>>
    {
        public int Compare(StableConnectionElement<TApi> x, StableConnectionElement<TApi> y)
        {
            if (object.ReferenceEquals(x, null) && object.ReferenceEquals(y, null))
                return 0;

            if (object.ReferenceEquals(x, null))
                return -1;

            if (object.ReferenceEquals(y, null))
                return 1;

            if (!x.CanBeUsedForCommunication && !y.CanBeUsedForCommunication)
                return y.ConcurrentRequestCount.CompareTo(x.ConcurrentRequestCount);

            if (!x.CanBeUsedForCommunication)
                return -1;

            if (!y.CanBeUsedForCommunication)
                return 1;

            return y.ConcurrentRequestCount.CompareTo(x.ConcurrentRequestCount);
        }
    }

    internal class StableElementsDynamicConnectionPool<TApi> : BalancingDynamicSizePoolManager<StableConnectionElement<TApi>, PoolElement<StableConnectionElement<TApi>>>
    {
        private readonly ChannelFactory<TApi> _factory;
        private readonly int _maxAsyncQueryCount;
        private readonly string _targetName;
        private volatile bool _isSyncOpen = true;
        public readonly int DeadlockTimeout = 60 * 1000;

        public StableElementsDynamicConnectionPool(ChannelFactory<TApi> factory, int maxAsyncQueryCount, int maxElementCount, int trimPeriod, string name)
            : base(maxElementCount, trimPeriod, new StableConnectionElementComparer<TApi>(), name)
        {
            Contract.Requires(factory != null);

            _factory = factory;
            _maxAsyncQueryCount = maxAsyncQueryCount;
            _targetName = factory.Endpoint.Address.ToString();
        }
        public StableElementsDynamicConnectionPool(ChannelFactory<TApi> factory, int maxAsyncQueryCount, int maxElementCount, int trimPeriod)
            : this(factory, maxAsyncQueryCount, maxElementCount, trimPeriod, null)
        {
        }
        public StableElementsDynamicConnectionPool(ChannelFactory<TApi> factory, int maxAsyncQueryCount, int maxElementCount)
            : this(factory, maxAsyncQueryCount, maxElementCount, 5 * 60 * 1000, null)
        {
        }        

        protected override PoolElement<StableConnectionElement<TApi>> CreatePoolElement(StableConnectionElement<TApi> elem)
        {
            return new PoolElement<StableConnectionElement<TApi>>(this, elem);
        }

        protected override bool CreateElement(out StableConnectionElement<TApi> elem, int timeout, CancellationToken token)
        {
            elem = new StableConnectionElement<TApi>(_factory, _maxAsyncQueryCount, _isSyncOpen);
            return true;
        }
        protected override void DestroyElement(StableConnectionElement<TApi> elem)
        {
            if (elem == null)
                return;

            elem.Dispose();
        }
        protected override bool IsValidElement(StableConnectionElement<TApi> elem)
        {
            return elem != null;
        }

        protected override bool IsBetterAllocateNew(StableConnectionElement<TApi> elem)
        {
            return elem == null || !elem.CanBeUsedForCommunication;
        }
    }
}

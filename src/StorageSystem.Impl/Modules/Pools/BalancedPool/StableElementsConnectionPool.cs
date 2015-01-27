using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.ServiceModel;
using System.Threading;
using Qoollo.Turbo.ObjectPools;

namespace Qoollo.Impl.Modules.Pools.BalancedPool
{
    internal class StableConnectionElement<TApi> : StableConcurrentConnection<TApi>
    {
        public StableConnectionElement(ChannelFactory<TApi> factory, int maxAsyncQueryCount, bool syncFirstOpen = false)
            : base(factory, maxAsyncQueryCount, syncFirstOpen)
        {
        }
    }


    /// <summary>
    /// Сравнение элементов пула для выбора элементов с минимальным числом асинхронных запросов
    /// </summary>
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

        public static int CompareItems(StableConnectionElement<TApi> x, StableConnectionElement<TApi> y)
        {
            bool tmp = false;
            return CompareItems(x, y, out tmp);
        }

        public static int CompareItems(StableConnectionElement<TApi> x, StableConnectionElement<TApi> y, out bool stopHere)
        {
            if (object.ReferenceEquals(x, null) && object.ReferenceEquals(y, null))
            {
                stopHere = false;
                return 0;
            }

            if (object.ReferenceEquals(x, null))
            {
                stopHere = y.CanBeUsedForCommunication && y.ConcurrentRequestCount == 0;
                return -1;
            }

            if (object.ReferenceEquals(y, null))
            {
                stopHere = x.CanBeUsedForCommunication && x.ConcurrentRequestCount == 0;
                return 1;
            }

            if (!x.CanBeUsedForCommunication && !y.CanBeUsedForCommunication)
            {
                stopHere = false;
                return y.ConcurrentRequestCount.CompareTo(x.ConcurrentRequestCount);
            }

            if (!x.CanBeUsedForCommunication)
            {
                stopHere = y.ConcurrentRequestCount == 0;
                return -1;
            }

            if (!y.CanBeUsedForCommunication)
            {
                stopHere = x.ConcurrentRequestCount == 0;
                return 1;
            }

            if (y.ConcurrentRequestCount > x.ConcurrentRequestCount)
            {
                stopHere = x.ConcurrentRequestCount == 0;
                return 1;
            }

            if (y.ConcurrentRequestCount < x.ConcurrentRequestCount)
            {
                stopHere = y.ConcurrentRequestCount == 0;
                return -1;
            }

            stopHere = x.ConcurrentRequestCount == 0;
            return 0;
        }
    }

    internal class StableElementsDynamicConnectionPool<TApi> : BalancingDynamicPoolManager<StableConnectionElement<TApi>>
    {
        private readonly ChannelFactory<TApi> _factory;
        private readonly int _maxAsyncQueryCount;
        private volatile bool _isSyncOpen = true;
        public readonly int DeadlockTimeout = 60 * 1000;

        public StableElementsDynamicConnectionPool(ChannelFactory<TApi> factory, int maxAsyncQueryCount,
            int maxElementCount, int trimPeriod, string name)
            : base(1, maxElementCount, name, trimPeriod)
        {
            Contract.Requires(factory != null);

            _factory = factory;
            _maxAsyncQueryCount = maxAsyncQueryCount;
        }

        public StableElementsDynamicConnectionPool(ChannelFactory<TApi> factory, int maxAsyncQueryCount,
            int maxElementCount, int trimPeriod)
            : this(factory, maxAsyncQueryCount, maxElementCount, trimPeriod, null)
        {
        }

        public StableElementsDynamicConnectionPool(ChannelFactory<TApi> factory, int maxAsyncQueryCount,
            int maxElementCount)
            : this(factory, maxAsyncQueryCount, maxElementCount, 5 * 60 * 1000, null)
        {
        }

        protected override bool CreateElement(out StableConnectionElement<TApi> elem, int timeout,
            CancellationToken token)
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

        protected override int CompareElements(StableConnectionElement<TApi> a, StableConnectionElement<TApi> b, out bool stopHere)
        {
            return StableConnectionElementComparer<TApi>.CompareItems(a, b, out stopHere);
        }

        protected override bool IsBetterAllocateNew(StableConnectionElement<TApi> elem)
        {
            return elem == null || !elem.CanBeUsedForCommunication;
        }
    }
}

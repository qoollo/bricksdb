using Libs.PerformanceCounters;

namespace Qoollo.Impl.Proxy.PerfCounters
{        
    [PerfCountersContainer]
    internal class ProxyCounters : SingleInstanceCategoryWrapper
    {
        #region Singleton

        public ProxyCounters()
            : base("StorageSystem.Proxy", "Счётчики для прокси")
        {
        }

        private static volatile ProxyCounters _instance =
            PerfCountersDefault.NullCounterFactory.CreateCategoryWrapper<ProxyCounters>();

        public static ProxyCounters Instance
        {
            get
            {
                return _instance;
            }
        }

        [PerfCountersInitializationMethod]
        public static void Init(CategoryWrapper wrapper)
        {
            _instance = wrapper.CreateSubCategory<ProxyCounters>();
        }

        #endregion

        protected override void AfterInit()
        {
            AverageTimer.Reset();
            CreateCount.Reset();
            ReadCount.Reset();
            DeleteCount.Reset();
            UpdateCount.Reset();
            IncomePerSec.Reset();
            AllProcessPerSec.Reset();
            AsyncProcessPerSec.Reset();
            SyncProcessPerSec.Reset();
            CustomOperationCount.Reset();
        }

        [Counter("CreateCount", "Counter")]
        public NumberOfItemsCounter CreateCount { get; private set; }

        [Counter("UpdateCount", "Counter")]
        public NumberOfItemsCounter UpdateCount { get; private set; }

        [Counter("ReadCount", "Counter")]
        public NumberOfItemsCounter ReadCount { get; private set; }

        [Counter("DeleteCount", "Counter")]
        public NumberOfItemsCounter DeleteCount { get; private set; }

        [Counter("CustomOperationCount", "Counter")]
        public NumberOfItemsCounter CustomOperationCount { get; private set; }

        [Counter("Avg operation time in Proxy befor sending to Distributor")]
        public AverageTimeCounter AverageTimer { get; private set; }

        [Counter("Count operation from client per sec")]
        public OperationsPerSecondCounter IncomePerSec { get; private set; }

        [Counter("Count operation proccesed per sec")]
        public OperationsPerSecondCounter AllProcessPerSec { get; private set; }

        [Counter("Count sync operation proccesed per sec")]
        public OperationsPerSecondCounter SyncProcessPerSec { get; private set; }

        [Counter("Count async operation proccesed per sec")]
        public OperationsPerSecondCounter AsyncProcessPerSec { get; private set; }
    }
}
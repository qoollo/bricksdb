using Qoollo.PerformanceCounters;

namespace Qoollo.Impl.DistributorModules.PerfCounters
{
    [PerfCountersContainer]
    internal class DistributorCounters : SingleInstanceCategoryWrapper
    {
        public DistributorCounters()
            : base("StorageSystem.Distributor", "Counters Distributor")
        {
        }

        private static volatile DistributorCounters _instance = 
            PerfCountersDefault.NullCounterFactory.CreateCategoryWrapper<DistributorCounters>();

        public static DistributorCounters Instance
        {
            get
            {
                return _instance;
            }
        }

        [PerfCountersInitializationMethod]
        public static void Init(CategoryWrapper wrapper)
        {
            _instance = wrapper.CreateSubCategory<DistributorCounters>();
        }

        protected override void AfterInit()
        {
            AverageTimer.Reset();
            TransactionCount.Reset();
            TransactionFailCount.Reset();
            IncomePerSec.Reset();
            ProcessPerSec.Reset();
        }

        [Counter("TransactionCount", "Counter")]
        public NumberOfItemsCounter TransactionCount { get; private set; }

        [Counter("TransactionFailCount", "Counter")]
        public NumberOfItemsCounter TransactionFailCount { get; private set; }        
        
        [Counter("Avg operation time")]
        public AverageTimeCounter AverageTimer { get; private set; }

        [Counter("Count operation from Proxy per sec")]
        public OperationsPerSecondCounter IncomePerSec { get; private set; }

        [Counter("Processed operation per sec")]
        public OperationsPerSecondCounter ProcessPerSec { get; private set; }
    }
}
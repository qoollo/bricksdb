using Libs.PerformanceCounters;

namespace Qoollo.Impl.DbController.PerfCounters
{
    [PerfCountersContainer] 
    internal class DbControllerCounters : SingleInstanceCategoryWrapper
    {
        #region Singleton

        public DbControllerCounters()
            : base("StorageSystem.DbController", "Counters for DbController")
        {
        }

        private static volatile DbControllerCounters _instance =
            PerfCountersDefault.NullCounterFactory.CreateCategoryWrapper<DbControllerCounters>();

        public static DbControllerCounters Instance
        {
            get
            {
                return _instance;
            }
        }

        [PerfCountersInitializationMethod]
        public static void Init(CategoryWrapper wrapper)
        {
            _instance = wrapper.CreateSubCategory<DbControllerCounters>();
        }

        #endregion

        protected override void AfterInit()
        {
            CreatePerSec.Reset();
            UpdatePerSec.Reset();
            ReadPerSec.Reset();
            DeletePerSec.Reset();
            RestoreUpdatePerSec.Reset();
            CustomOperationPerSec.Reset();
            AverageTimer.Reset();
            TransactionCount.Reset();
            ProcessPerSec.Reset();
            IncomePerSec.Reset();

            CreateMetaDataTimer.Reset();
            ReadMetaDataTimer.Reset();
            CreateTimer.Reset();
        }
        
        [Counter("Count create operation (Per/sec)")]
        public OperationsPerSecondCounter CreatePerSec { get; private set; }

        [Counter("Count update operations (Per/sec)")]
        public OperationsPerSecondCounter UpdatePerSec { get; private set; }

        [Counter("Count read operations (Per/sec)")]
        public OperationsPerSecondCounter ReadPerSec { get; private set; }

        [Counter("Count delete operations(Per/sec)")]
        public OperationsPerSecondCounter DeletePerSec { get; private set; }

        [Counter("Count restoreUpdate operations(Per/sec)")]
        public OperationsPerSecondCounter RestoreUpdatePerSec { get; private set; }

        [Counter("Count customOperation operations(Per/sec)")]
        public OperationsPerSecondCounter CustomOperationPerSec { get; private set; }

        [Counter("Total operation count", "Числовой счетчик")]
        public NumberOfItemsCounter TransactionCount { get; private set; }      
        
        [Counter("Avg operations time")]
        public AverageTimeCounter AverageTimer { get; private set; }

        [Counter("Avg operations time with queue waiting")]
        public AverageTimeCounter AverageTimerWithQueue { get; private set; }

        [Counter("Count operations (Per/sec)")]
        public OperationsPerSecondCounter ProcessPerSec { get; private set; }

        [Counter("Count income operations")]
        public OperationsPerSecondCounter IncomePerSec { get; private set; }

        #region TimerFor Create

        [Counter("ReadMetaDataTimer")]
        public AverageTimeCounter ReadMetaDataTimer { get; private set; }

        [Counter("CreateTimer")]
        public AverageTimeCounter CreateTimer { get; private set; }

        [Counter("CreateMetaDataTimer")]
        public AverageTimeCounter CreateMetaDataTimer { get; private set; }

        #endregion
    }
}
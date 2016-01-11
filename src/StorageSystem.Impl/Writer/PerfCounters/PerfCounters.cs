using Qoollo.Impl.PerfCounters;
using Qoollo.PerformanceCounters;

namespace Qoollo.Impl.Writer.PerfCounters
{
    [PerfCountersContainer] 
    internal class WriterCounters : SingleInstanceCategoryWrapper
    {
        #region Singleton

        public WriterCounters()
            : base("StorageSystem.Writer", "Counters for Writer")
        {
        }

        private static volatile WriterCounters _instance =
            PerfCountersDefault.NullCounterFactory.CreateCategoryWrapper<WriterCounters>();

        public static WriterCounters Instance
        {
            get
            {
                return _instance;
            }
        }

        [PerfCountersInitializationMethod]
        public static void Init(CategoryWrapper wrapper)
        {
            _instance = wrapper.CreateSubCategory<WriterCounters>();
            ServiceClassesPerfCountersProfiling.Init(_instance);
        }

        #endregion

        protected override void AfterInit()
        {
            ResetAllCounters();
        }

        [Counter("TransactionCount", "Общее количество обработанных данных")]
        public NumberOfItemsCounter TransactionCount { get; private set; }

        [Counter("DeleteTimeoutPerSec", "Количество удаляемых событий из базы Delete Timeout(Per/sec)")]
        public OperationsPerSecondCounter DeleteTimeoutPerSec { get; private set; }

        [Counter("DeleteFullPerSec", "Количество сразу удаляемых событий из базы Delete Full(Per/sec)")]
        public OperationsPerSecondCounter DeleteFullPerSec { get; private set; }

        #region Restore

        [Counter("RestoreCheckCount", "Количество проверенных данных при восстановлении")]
        public NumberOfItemsCounter RestoreCheckCount { get; private set; }

        [Counter("RestoreCountSend", "Количество переданных данных при восстановлении")]
        public NumberOfItemsCounter RestoreCountSend { get; private set; }

        [Counter("RestoreCountReceive", "Количество принятых данных во время восстановления")]
        public NumberOfItemsCounter RestoreCountReceive { get; private set; }

        [Counter("RestoreUpdatePerSec", "Количество обрабатываемых транзакций RestoreUpdate (Per/sec)")]
        public OperationsPerSecondCounter RestoreUpdatePerSec { get; private set; }

        [Counter("RestoreUpdatePerSecPackage", "Количество обрабатываемых транзакций RestoreUpdatePackage (Per/sec)")]
        public OperationsPerSecondCounter RestoreUpdatePerSecPackage { get; private set; }

        [Counter("RestoreSendPerSec", "Количество отправленных RestoreUpdate (Per/sec)")]
        public OperationsPerSecondCounter RestoreSendPerSec { get; private set; }

        [Counter("RestoreCheckPerSec", "Количество проверенных данных при восстановлении RestoreUpdate (Per/sec)")]
        public OperationsPerSecondCounter RestoreCheckPerSec { get; private set; }

        #endregion

        #region Crud Per sec

        [Counter("Количество поисковых запросов (Per/sec)")]
        public OperationsPerSecondCounter QueryPerSec { get; private set; }

        [Counter("Количество обрабатываемых транзакций в секунду")]
        public OperationsPerSecondCounter ProcessPerSec { get; private set; }

        [Counter("Количество принятых транзакций в секунду")]
        public OperationsPerSecondCounter IncomePerSec { get; private set; }

        [Counter("Количество обрабатываемых транзакций Create (Per/sec)")]
        public OperationsPerSecondCounter CreatePerSec { get; private set; }

        [Counter("Количество обрабатываемых транзакций Update (Per/sec)")]
        public OperationsPerSecondCounter UpdatePerSec { get; private set; }

        [Counter("Количество обрабатываемых транзакций Read (Per/sec)")]
        public OperationsPerSecondCounter ReadPerSec { get; private set; }

        [Counter("Количество обрабатываемых транзакций Delete (Per/sec)")]
        public OperationsPerSecondCounter DeletePerSec { get; private set; }
        
        [Counter("Количество обрабатываемых транзакций CustomOperation (Per/sec)")]
        public OperationsPerSecondCounter CustomOperationPerSec { get; private set; }

        #endregion

        #region Timers

        [Counter("Время обработки запроса")]
        public AverageTimeCounter QueryAvgTime { get; private set; }

        [Counter("ReadMetaDataTimer")]
        public AverageTimeCounter ReadMetaDataTimer { get; private set; }

        [Counter("CreateTimer")]
        public AverageTimeCounter CreateTimer { get; private set; }

        [Counter("CreateMetaDataTimer")]
        public AverageTimeCounter CreateMetaDataTimer { get; private set; }

        [Counter("Среднее время транзакции")]
        public AverageTimeCounter AverageTimer { get; private set; }

        [Counter("Среднее время Read")]
        public AverageTimeCounter ReadTimer { get; private set; }

        [Counter("Среднее время транзакции с очередью")]
        public AverageTimeCounter AverageTimerWithQueue { get; private set; }

        #endregion
    }
}
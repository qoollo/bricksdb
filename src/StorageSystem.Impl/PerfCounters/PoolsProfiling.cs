using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.PerformanceCounters;

namespace Qoollo.Impl.PerfCounters
{
    internal static class PoolsProfiler
    {
        public static void SetProfiler(Qoollo.Logger.Logger logger)
        {
            Qoollo.Turbo.Profiling.Profiler.SetProfiler(new ServiceClassesProfilerWrapper(ServiceClassesPerfCountersProfiling.Instance, logger));
        }
    }

    internal class ServiceClassesPerfCountersProfiling : Qoollo.Turbo.Profiling.IProfilingProvider
    {
        #region Init

        private static ServiceClassesPerfCountersProfiling _instance = null;

        public static ServiceClassesPerfCountersProfiling Instance
        {
            get { return _instance; }
        }

        public static void Init(CategoryWrapper root)
        {
            _instance = new ServiceClassesPerfCountersProfiling(root);
        }

        #endregion

        private readonly ServiceClassesCategory _srvClsCat;
        private readonly PoolCategory _srvClsPoolCat;
        private readonly QueueAsyncProcCategory _srcClsAsyncQueueProcCat;
        private readonly ThreadPoolCategory _srvClsThreadPoolCat;

        private ServiceClassesPerfCountersProfiling(CategoryWrapper rootCat)
        {
            _srvClsCat = rootCat.CreateSubCategory<ServiceClassesCategory>();
            _srvClsPoolCat = _srvClsCat.CreateSubCategory<PoolCategory>();
            _srcClsAsyncQueueProcCat = _srvClsCat.CreateSubCategory<QueueAsyncProcCategory>();
            _srvClsThreadPoolCat = _srvClsCat.CreateSubCategory<ThreadPoolCategory>();
        }

        #region Pool

        public void ObjectPoolCreated(string poolName)
        {
        }

        public void ObjectPoolDisposed(string poolName, bool fromFinalizer)
        {
            _srvClsPoolCat[poolName].Remove();
        }

        public void ObjectPoolElementCreated(string poolName, int currentElementCount)
        {
            var poolInst = _srvClsPoolCat[poolName ?? "-"];
            poolInst.CurrentActiveElementCount.SetValue(currentElementCount);
            poolInst.TotalCreatedElementCount.Increment();
        }

        public void ObjectPoolElementDestroyed(string poolName, int currentElementCount)
        {
            var poolInst = _srvClsPoolCat[poolName ?? "-"];
            poolInst.CurrentActiveElementCount.SetValue(currentElementCount);
            poolInst.TotalDestroyedElementCount.Increment();
        }

        public void ObjectPoolElementFaulted(string poolName, int currentElementCount)
        {
            var poolInst = _srvClsPoolCat[poolName ?? "-"];
            poolInst.CurrentActiveElementCount.SetValue(currentElementCount);
            poolInst.TotalFaultedElementCount.Increment();
        }

        public void ObjectPoolElementReleased(string poolName, int currentRentedCount)
        {
            _srvClsPoolCat[poolName ?? "-"].RentedElementCount.SetValue(currentRentedCount);
        }

        public void ObjectPoolElementRented(string poolName, int currentRentedCount)
        {
            var poolInst = _srvClsPoolCat[poolName ?? "-"];
            poolInst.RentedElementCount.SetValue(currentRentedCount);
            poolInst.RentsPerSecond.OperationFinished();
        }

        public void ObjectPoolElementRentedTime(string poolName, TimeSpan time)
        {
            _srvClsPoolCat[poolName ?? "-"].AverageElementRentTime.RegisterMeasurement(time);
        }

        #endregion

        #region Queue

        public void QueueAsyncProcessorCreated(string queueProcName)
        {
        }

        public void QueueAsyncProcessorDisposed(string queueProcName, bool fromFinalizer)
        {
            _srcClsAsyncQueueProcCat[queueProcName].Remove();
        }

        public void QueueAsyncProcessorElementProcessed(string queueProcName, TimeSpan time)
        {
            var asyncProcInst = _srcClsAsyncQueueProcCat[queueProcName];
            asyncProcInst.AverageElementProcessTime.RegisterMeasurement(time);
            asyncProcInst.ProcessedElementsPerSecond.OperationFinished();
            asyncProcInst.TotalProcessedElementCount.Increment();
        }


        public void QueueAsyncProcessorThreadStart(string queueProcName, int curThreadCount, int expectedThreadCount)
        {
            var asyncProcInst = _srcClsAsyncQueueProcCat[queueProcName];
            asyncProcInst.CurrentThreadCount.SetValue(curThreadCount);
            asyncProcInst.ExpectedThreadCount.SetValue(expectedThreadCount);
        }

        public void QueueAsyncProcessorThreadStop(string queueProcName, int curThreadCount, int expectedThreadCount)
        {
            var asyncProcInst = _srcClsAsyncQueueProcCat[queueProcName];
            asyncProcInst.CurrentThreadCount.SetValue(curThreadCount);
            asyncProcInst.ExpectedThreadCount.SetValue(expectedThreadCount);
        }


        public void QueueAsyncProcessorElementCountIncreased(string queueProcName, int newElementCount, int maxElementCount)
        {
            var asyncProcInst = _srcClsAsyncQueueProcCat[queueProcName];
            asyncProcInst.MaxAvailableElementQueueSize.SetValue(maxElementCount);
            asyncProcInst.CurrentElementQueueSize.SetValue(newElementCount);
            asyncProcInst.AverageElementQueueSize.RegisterValue(newElementCount);
            asyncProcInst.NewElementsPerSecond.OperationFinished();
            if (newElementCount > asyncProcInst.MaxElementQueueSize.CurrentValue)
                asyncProcInst.MaxElementQueueSize.SetValue(newElementCount);
        }

        public void QueueAsyncProcessorElementCountDecreased(string queueProcName, int newElementCount, int maxElementCount)
        {
            var asyncProcInst = _srcClsAsyncQueueProcCat[queueProcName];
            asyncProcInst.CurrentElementQueueSize.SetValue(newElementCount);
            asyncProcInst.AverageElementQueueSize.RegisterValue(newElementCount);
        }

        public void QueueAsyncProcessorElementRejectedInTryAdd(string queueProcName, int currentElementCount)
        {
            var asyncProcInst = _srcClsAsyncQueueProcCat[queueProcName];
            asyncProcInst.CurrentElementQueueSize.SetValue(currentElementCount);
            asyncProcInst.TotalRejectedInTryAddElementCount.Increment();
        }

        #endregion

        #region Thread

        public void ThreadPoolCreated(string threadPoolName)
        {
        }

        public void ThreadPoolDisposed(string threadPoolName, bool fromFinalizer)
        {
            _srvClsThreadPoolCat[threadPoolName].Remove();
        }

        public void ThreadPoolThreadCountChange(string threadPoolName, int curThreadCount)
        {
            var threadPoolInst = _srvClsThreadPoolCat[threadPoolName];
            threadPoolInst.CurrentThreadCount.SetValue(curThreadCount);
        }

        public void ThreadPoolWorkCancelled(string threadPoolName)
        {
            _srvClsThreadPoolCat[threadPoolName].TotalCancelledTaskCount.Increment();
        }

        public void ThreadPoolWorkItemRejectedInTryAdd(string threadPoolName)
        {
            _srvClsThreadPoolCat[threadPoolName].TotalRejectedInTryAddTaskCount.Increment();
        }

        public void ThreadPoolWorkProcessed(string threadPoolName, TimeSpan time)
        {
            var threadPoolInst = _srvClsThreadPoolCat[threadPoolName];
            threadPoolInst.AverageTaskProcessTime.RegisterMeasurement(time);
            threadPoolInst.ProcessedTasksPerSecond.OperationFinished();
            threadPoolInst.TotalProcessedTaskCount.Increment();
        }


        public void ThreadPoolWaitingInQueueTime(string threadPoolName, TimeSpan time)
        {
            var threadPoolInst = _srvClsThreadPoolCat[threadPoolName];
            threadPoolInst.AverageTaskWaitingTime.RegisterMeasurement(time);
        }

        public void ThreadPoolWorkItemsCountIncreased(string threadPoolName, int newItemCount, int maxItemCount)
        {
            var threadPoolInst = _srvClsThreadPoolCat[threadPoolName];
            threadPoolInst.MaxAvailableTaskQueueSize.SetValue(maxItemCount);
            threadPoolInst.CurrentTaskQueueSize.SetValue(newItemCount);
            threadPoolInst.AverageTaskQueueSize.RegisterValue(newItemCount);
            threadPoolInst.NewTasksPerSecond.OperationFinished();
            if (newItemCount > threadPoolInst.MaxTaskQueueSize.CurrentValue)
                threadPoolInst.MaxTaskQueueSize.SetValue(newItemCount);
        }

        public void ThreadPoolWorkItemsCountDecreased(string threadPoolName, int newItemCount, int maxItemCount)
        {
            var threadPoolInst = _srvClsThreadPoolCat[threadPoolName];
            threadPoolInst.CurrentTaskQueueSize.SetValue(newItemCount);
            threadPoolInst.AverageTaskQueueSize.RegisterValue(newItemCount);
        }

        #endregion

        #region Not implemented

        public void ThreadSetManagerCreated(string threadSetManagerName, int initialThreadCount)
        {
            throw new NotImplementedException();
        }

        public void ThreadSetManagerDisposed(string threadSetManagerName, bool fromFinalizer)
        {
            throw new NotImplementedException();
        }

        public void ThreadSetManagerThreadStart(string threadSetManagerName, int curThreadCount, int expectedThreadCount)
        {
            throw new NotImplementedException();
        }

        public void ThreadSetManagerThreadStop(string threadSetManagerName, int curThreadCount, int expectedThreadCount)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    internal class ServiceClassesCategory : EmptyCategoryWrapper
    {
        public ServiceClassesCategory()
            : base("ServiceClasses", "Профилировка для пулов")
        {
        }
    }

    internal class PoolCategoryInstance : InstanceInMultiInstanceCategoryWrapper
    {
        protected override void AfterInit()
        {
            this.ResetAllCounters();
        }


        /// <summary>
        /// Среднее время нахождения элемента пула в аренде
        /// </summary>
        [Counter("Среднее время нахождения элемента пула в аренде")]
        public AverageTimeCounter AverageElementRentTime { get; private set; }



        /// <summary>
        /// Число арендованных элементов
        /// </summary>
        [Counter("Число арендованных элементов")]
        public NumberOfItemsCounter RentedElementCount { get; private set; }

        /// <summary>
        /// Количество арендуемых элементов в секунду
        /// </summary>
        [Counter("Количество арендуемых элементов в секунду")]
        public OperationsPerSecondCounter RentsPerSecond { get; private set; }


        /// <summary>
        /// Общее число созданных пулом элементов
        /// </summary>
        [Counter("Общее число созданных пулом элементов")]
        public NumberOfItemsCounter TotalCreatedElementCount { get; private set; }

        /// <summary>
        /// Общее число элементов, ставших невалидными
        /// </summary>
        [Counter("Общее число элементов, ставших невалидными")]
        public NumberOfItemsCounter TotalFaultedElementCount { get; private set; }

        /// <summary>
        /// Общее число уничтоженных пулом элементов
        /// </summary>
        [Counter("Общее число уничтоженных пулом элементов")]
        public NumberOfItemsCounter TotalDestroyedElementCount { get; private set; }

        /// <summary>
        /// Число активных элементов в пуле в данный момент
        /// </summary>
        [Counter("Число активных элементов в пуле в данный момент")]
        public NumberOfItemsCounter CurrentActiveElementCount { get; private set; }
    }    

    internal class PoolCategory : MultiInstanceCategoryWrapper<PoolCategoryInstance>
    {
        public PoolCategory()
            : base("Pool", "Профилировка пулов элементов")
        {
        }
    }

    internal class QueueAsyncProcCategoryInstance : InstanceInMultiInstanceCategoryWrapper
    {
        protected override void AfterInit()
        {
            this.ResetAllCounters();
        }

        /// <summary>
        /// Ожидаемое (максимальное) число потоков обработки
        /// </summary>
        [Counter("Ожидаемое (максимальное) число потоков обработки")]
        public NumberOfItemsCounter ExpectedThreadCount { get; private set; }

        /// <summary>
        /// Текущее число потоков обработки
        /// </summary>
        [Counter("Текущее число потоков обработки")]
        public NumberOfItemsCounter CurrentThreadCount { get; private set; }


        /// <summary>
        /// Среднее время обработки элемента
        /// </summary>
        [Counter("Среднее время обработки элемента")]
        public AverageTimeCounter AverageElementProcessTime { get; private set; }


        /// <summary>
        /// Общее число обработанных элементов (за всё время)
        /// </summary>
        [Counter("Общее число обработанных элементов (за всё время)")]
        public NumberOfItemsCounter TotalProcessedElementCount { get; private set; }


        /// <summary>
        /// Общее число отброшенных элементов из-за заполненности очереди (за всё время)
        /// </summary>
        [Counter("Общее число отброшенных элементов из-за заполненности очереди (за всё время)")]
        public NumberOfItemsCounter TotalRejectedInTryAddElementCount { get; private set; }

        /// <summary>
        /// Максимально разрешённая длина очереди
        /// </summary>
        [Counter("Максимально разрешённая длина очереди")]
        public NumberOfItemsCounter MaxAvailableElementQueueSize { get; private set; }

        /// <summary>
        /// Максимальная длина очереди за время работы
        /// </summary>
        [Counter("Максимальная длина очереди за время работы")]
        public NumberOfItemsCounter MaxElementQueueSize { get; private set; }

        /// <summary>
        /// Текущая длина очереди
        /// </summary>
        [Counter("Текущая длина очереди")]
        public NumberOfItemsCounter CurrentElementQueueSize { get; private set; }

        /// <summary>
        /// Средняя длина очереди
        /// </summary>
        [Counter("Средняя длина очереди")]
        public AverageCountCounter AverageElementQueueSize { get; private set; }


        /// <summary>
        /// Скорость добавления (Число новых элементов в секунду)
        /// </summary>
        [Counter("Скорость добавления (Число новых элементов в секунду)")]
        public OperationsPerSecondCounter NewElementsPerSecond { get; private set; }

        /// <summary>
        /// Скорость обработки (Число обрабатываемых элементов в секунду)
        /// </summary>
        [Counter("Скорость обработки (Число обрабатываемых элементов в секунду)")]
        public OperationsPerSecondCounter ProcessedElementsPerSecond { get; private set; }
    }

    internal class QueueAsyncProcCategory : MultiInstanceCategoryWrapper<QueueAsyncProcCategoryInstance>
    {
        public QueueAsyncProcCategory()
            : base("AsyncQueueProcessor", "Профилировка асинхронного обработчика с очередью")
        {
        }
    }

    internal class ThreadPoolCategoryInstance : InstanceInMultiInstanceCategoryWrapper
    {
        protected override void AfterInit()
        {
            this.ResetAllCounters();
        }


        /// <summary>
        /// Текущее число потоков в пуле
        /// </summary>
        [Counter("Текущее число потоков в пуле")]
        public NumberOfItemsCounter CurrentThreadCount { get; private set; }


        /// <summary>
        /// Среднее время обработки задачи
        /// </summary>
        [Counter("Среднее время обработки задачи")]
        public AverageTimeCounter AverageTaskProcessTime { get; private set; }


        /// <summary>
        /// Общее число обработанных задач (за всё время)
        /// </summary>
        [Counter("Общее число обработанных задач (за всё время)")]
        public NumberOfItemsCounter TotalProcessedTaskCount { get; private set; }

        /// <summary>
        /// Число отменённых задач
        /// </summary>
        [Counter("Число отменённых задач")]
        public NumberOfItemsCounter TotalCancelledTaskCount { get; private set; }

        /// <summary>
        /// Общее число отброшенных элементов из-за заполненности очереди (за всё время)
        /// </summary>
        [Counter("Общее число отброшенных элементов из-за заполненности очереди (за всё время)")]
        public NumberOfItemsCounter TotalRejectedInTryAddTaskCount { get; private set; }

        /// <summary>
        /// Максимально разрешённая длина очереди
        /// </summary>
        [Counter("Максимально разрешённая длина очереди")]
        public NumberOfItemsCounter MaxAvailableTaskQueueSize { get; private set; }

        /// <summary>
        /// Максимальная длина очереди за время работы
        /// </summary>
        [Counter("Максимальная длина очереди за время работы")]
        public NumberOfItemsCounter MaxTaskQueueSize { get; private set; }

        /// <summary>
        /// Текущая длина очереди
        /// </summary>
        [Counter("Текущая длина очереди")]
        public NumberOfItemsCounter CurrentTaskQueueSize { get; private set; }

        /// <summary>
        /// Средняя длина очереди
        /// </summary>
        [Counter("Средняя длина очереди")]
        public AverageCountCounter AverageTaskQueueSize { get; private set; }


        /// <summary>
        /// Скорость добавления (Число новых элементов в секунду)
        /// </summary>
        [Counter("Скорость добавления (Число новых элементов в секунду)")]
        public OperationsPerSecondCounter NewTasksPerSecond { get; private set; }

        /// <summary>
        /// Скорость обработки (Число обрабатываемых элементов в секунду)
        /// </summary>
        [Counter("Скорость обработки (Число обрабатываемых элементов в секунду)")]
        public OperationsPerSecondCounter ProcessedTasksPerSecond { get; private set; }


        /// <summary>
        /// Среднее время нахождения задачи в очереди
        /// </summary>
        [Counter("Среднее время нахождения задачи в очереди")]
        public AverageTimeCounter AverageTaskWaitingTime { get; private set; }
    }

    internal class ThreadPoolCategory : MultiInstanceCategoryWrapper<ThreadPoolCategoryInstance>
    {
        public ThreadPoolCategory()
            : base("ThreadPool", "Профилировка пулов потоков")
        {
        }
    }

    internal class ServiceClassesProfilerWrapper : Qoollo.Turbo.Profiling.ProfilingProviderWrapper
    {
        private readonly Qoollo.Logger.Logger _logger;

        public ServiceClassesProfilerWrapper(Qoollo.Turbo.Profiling.IProfilingProvider provider, Qoollo.Logger.Logger logger)
            : base(provider)
        {
            if (logger == null)
                throw new ArgumentNullException("logger");

            _logger = logger;
        }


        protected override void ProcessException(Exception ex)
        {
            _logger.Error(ex, "Unexpected ProfilingProviderError! Fix this!");
        }
    }
}

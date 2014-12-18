using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.ServiceClasses.ThreadPools;
using Qoollo.Impl.Collector.Tasks;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;

namespace Qoollo.Impl.Collector.Background
{
    internal class BackgroundModule:ControlModule
    {
        private readonly DynamicThreadPool _threadPool;
        private readonly List<SearchTask> _tasks; 

        public BackgroundModule(QueueConfiguration queueConfiguration)
        {
            _tasks = new List<SearchTask>();
            _threadPool = new DynamicThreadPool(queueConfiguration.ProcessotCount, queueConfiguration.MaxSizeQueue, "BackgroundModule");
        }

        public void Run(SearchTask sTask, Action action)
        {
            _threadPool.Run(action);

            _tasks.Add(sTask);
        }

        public Task RunAsTask(Action action)
        {
            return _threadPool.RunAsTask(action);
        }

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                _tasks.ForEach(x=>x.Dispose());
                _threadPool.Dispose(false, false, false);
            }

            base.Dispose(isUserCall);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ninject;
using Qoollo.Impl.Collector.Interfaces;
using Qoollo.Impl.Collector.Tasks;
using Qoollo.Impl.Modules;

namespace Qoollo.Impl.Collector
{
    internal class BackgroundModule : ControlModule, IBackgroundModule
    {
        //private readonly DynamicThreadPool _threadPool;
        private readonly List<SearchTask> _tasks;

        public BackgroundModule(StandardKernel kernel)
            :base(kernel)
        {
            _tasks = new List<SearchTask>();
            //_threadPool = new DynamicThreadPool(1, queueConfiguration.ProcessotCount, queueConfiguration.MaxSizeQueue, "BackgroundModule");
        }

        public void Run(SearchTask sTask, Action action)
        {
            //_threadPool.Run(action);
            Task.Factory.StartNew(action);
            _tasks.Add(sTask);
        }

        public Task RunAsTask(Action action)
        {
            //return _threadPool.RunAsTask(action);
            return Task.Factory.StartNew(action);
        }

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                _tasks.ForEach(x => x.Dispose());
                //_threadPool.Dispose(false, false, false);
            }

            base.Dispose(isUserCall);
        }
    }
}

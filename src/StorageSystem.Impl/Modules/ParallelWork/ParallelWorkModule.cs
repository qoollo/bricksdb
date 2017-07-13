using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Ninject;
using Qoollo.Impl.Configurations.Queue;

namespace Qoollo.Impl.Modules.ParallelWork
{
    internal abstract class ParallelWorkModule<T>:ControlModule where T:class
    {
        private CancellationTokenSource _token;
        private BlockingCollection<T> _queue;
        private List<Thread> _threads;
        private List<SingleParallelWorkBase<T>> _workers;

        protected ParallelWorkModule(StandardKernel kernel)
            :base(kernel)
        {
            _token = new CancellationTokenSource();
            _queue = new BlockingCollection<T>();
            _threads = new List<Thread>();
            _workers = new List<SingleParallelWorkBase<T>>();                        
        }

        public override void Start()
        {
            var config = Kernel.Get<IDistributorConfiguration>();

            for (int i = 0; i < config.CountThreads; i++)
            {
                var thread = new Thread(Process);
                SingleParallelWorkBase<T> worker;
                if(!CreateWorker(out worker))
                    throw new Exception("initailization error");

                worker.Start();

                thread.Start(worker);

                _threads.Add(thread);
                _workers.Add(worker);
            }
        }

        public void Add(T data)
        {
            _queue.Add(data);
        }

        private void Process(object obj)
        {
            var worker = obj as SingleParallelWorkBase<T>;
            try
            {
                while (!_token.Token.IsCancellationRequested)
                {
                    T data = _queue.Take(_token.Token);
                    worker.Process(data);
                }
            }
            catch(OperationCanceledException)
            {}
            catch (Exception e)
            {
                Logger.Logger.Instance.Error(e,"");
            }            
        }

        protected abstract bool CreateWorker(out SingleParallelWorkBase<T> worker);

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                _token.Cancel();

                _threads.ForEach(x =>
                    {
                        if (!x.Join(1000))
                            x.Abort();
                    });
                _workers.ForEach(x=>x.Dispose());
            }

            base.Dispose(isUserCall);
        }
    }
}

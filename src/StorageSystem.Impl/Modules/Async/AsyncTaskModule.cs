using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Qoollo.Impl.Configurations;
using Qoollo.Turbo.Threading.ThreadPools;

namespace Qoollo.Impl.Modules.Async
{
    internal class AsyncTaskModule:ControlModule
    {
        private ReaderWriterLockSlim _lock;
        private List<AsyncData> _tasks;
        private DynamicThreadPool _threadPool;
        private Task _disp;
        private AutoResetEvent _event;
        private CancellationTokenSource _token;

        public AsyncTaskModule(QueueConfiguration configuration)
        {            
            _tasks = new List<AsyncData>();
            _threadPool = new DynamicThreadPool(1, configuration.ProcessotCount, configuration.MaxSizeQueue, "AsyncTaskModule");
            _lock = new ReaderWriterLockSlim();
            _event = new AutoResetEvent(false);
            _token = new CancellationTokenSource();
        }

        #region public 

        public void StopTask(string taskName)
        {
            _lock.EnterWriteLock();
            
            var task = _tasks.FirstOrDefault(x => x.ActionName == taskName);
            if(task!=null)
                task.Stop();

            _lock.ExitWriteLock();
        }

        public void DeleteTask(string taskName)
        {
            StopTask(taskName);

            _lock.EnterWriteLock();

            var task = _tasks.FirstOrDefault(x => x.ActionName == taskName);
            if (task != null)
                _tasks.Remove(task);

            _lock.ExitWriteLock();
        }

        public void RestartTask(string taskName, bool isForceStart = false)
        {
            _lock.EnterWriteLock();

            var task = _tasks.FirstOrDefault(x => x.ActionName == taskName);
            if (task != null)
            {
                task.Restart(isForceStart);
                _event.Set();
            }

            _lock.ExitWriteLock();
        }

        public void StartTask(string taskName, bool isForceStart = false)
        {
            _lock.EnterWriteLock();

            var task = _tasks.FirstOrDefault(x => x.ActionName == taskName);
            if (task != null)
            {
                task.Start(isForceStart);
                _event.Set();
            }

            _lock.ExitWriteLock();
        }

        public void AddAsyncTask(AsyncData asyncData, bool isforceStart)
        {
            _lock.EnterWriteLock();

            asyncData.GenerateNextTime(isforceStart);
            _tasks.Add(asyncData);
            _event.Set();

            _lock.ExitWriteLock();
        }

        #endregion

        #region private 

        private void ProcessTasks()
        {
            while (!_token.IsCancellationRequested)
            {
                var task = FindNextTask();

                DateTime nextTime = task == null ? DateTime.Now.AddMinutes(1) : task.Timeout;

                var now =  DateTime.Now;

                if (nextTime <= now)
                {
                    task.IncreaseCount();

                    _threadPool.Run(task.Action, task);

                    RemoveOldTasks();
                    task.GenerateNextTime(false);
                }
                else
                {
                    if ((int) (nextTime - now).TotalMilliseconds < 0)
                        _event.WaitOne(int.MaxValue);
                    else
                        _event.WaitOne(nextTime - now);

                    _event.Reset();
                }
            }
        }

        private void RemoveOldTasks()
        {
            _lock.EnterWriteLock();
            if (_tasks.Count != 0)
            {
                var tmp = _tasks.FindAll(x => !x.IsStopped && x.IsLast());
                if (tmp.Count != 0)
                {
                    tmp.ForEach(x=>_tasks.Remove(x));
                }
            }
            _lock.ExitWriteLock();
        }

        private AsyncData FindNextTask()
        {
            AsyncData ret = null;
            _lock.EnterReadLock();

            if (_tasks.Count != 0)
            {
                var tmp = _tasks.FindAll(x => !x.IsStopped && !x.IsLast());
                if (tmp.Count != 0)
                {
                    var time = tmp.Min(x => x.Timeout);
                    ret = _tasks.Find(x => x.Timeout == time && !x.IsStopped);
                }
            }
            _lock.ExitReadLock();

            return ret;
        }

        #endregion

        public override void Start()
        {     
            _disp = new Task(ProcessTasks);
            _disp.Start();
        }

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                _token.Cancel();
                _event.Set();                
                _threadPool.Dispose(false, false, false);
            }

            base.Dispose(isUserCall);
        }
    }
}

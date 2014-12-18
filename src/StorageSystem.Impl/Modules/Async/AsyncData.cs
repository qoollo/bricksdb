using System;
using System.Diagnostics.Contracts;

namespace Qoollo.Impl.Modules.Async
{
    internal abstract class AsyncData
    {
        public int TotalCount { get; private set; }
        public int CurrentCount { get; private set; }
        public Action<AsyncData> Action { get; private set; }
        public bool IsStopped { get; private set; }
        public string ActionName { get; private set; }

        public DateTime Timeout { get; protected set; }
        public abstract void GenerateNextTime(bool isforceStart);        

        protected AsyncData(Action<AsyncData> action, string name, int totalCount)
        {
            Contract.Requires(action != null);
            Contract.Requires(name != "");
            Action = action;
            IsStopped = false;
            ActionName = name;
            CurrentCount = 0;
            TotalCount = totalCount;
        }

        public void Stop()
        {
            IsStopped = true;
        }

        public void Restart(bool isForceStart)
        {
            CurrentCount = 0;
            GenerateNextTime(isForceStart);
            IsStopped = false;
        }

        public void Start(bool isForceStart = false)
        {
            GenerateNextTime(isForceStart);
            IsStopped = false;
        }

        public bool IsLast()
        {
            if (TotalCount == -1)
                return false;

            return CurrentCount >= TotalCount;
        }

        public void IncreaseCount()
        {
            CurrentCount++;
        }

        public override int GetHashCode()
        {
            return ActionName.GetHashCode();
        }
    }
}

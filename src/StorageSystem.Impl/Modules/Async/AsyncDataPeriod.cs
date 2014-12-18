using System;
using System.Diagnostics.Contracts;

namespace Qoollo.Impl.Modules.Async
{
    internal class AsyncDataPeriod:AsyncData
    {
        private TimeSpan _timeout;

        public AsyncDataPeriod(TimeSpan timeout, Action<AsyncData> action, string actionName, int totalCount)
            : base(action, actionName, totalCount)
        {
            Contract.Requires(timeout != null);
            _timeout = timeout;
        }

        public override void GenerateNextTime(bool isforceStart)
        {
            if (!isforceStart)
                Timeout = DateTime.Now + _timeout;
            else
                Timeout = DateTime.Now;
        }
    }
}

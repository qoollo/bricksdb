using System;
using System.Diagnostics.Contracts;

namespace Qoollo.Impl.Configurations
{
    internal class AsyncTasksConfiguration
    {
        public TimeSpan TimeoutPeriod { get;private set; }

        public AsyncTasksConfiguration(TimeSpan timeoutGetData)
        {
            Contract.Requires(timeoutGetData!=null);
            TimeoutPeriod = timeoutGetData;
        }
    }
}

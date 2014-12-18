using System;
using System.Diagnostics.Contracts;

namespace Qoollo.Impl.Configurations
{
    internal class RestoreModuleConfiguration
    {
        public int CountRetry { get; private set; }
        public TimeSpan PeriodRetry { get; private set; }
        public bool IsForceStart { get; private set; }
        public TimeSpan DeleteTimeout { get; private set; }

        public RestoreModuleConfiguration(int countRetry, TimeSpan periodRetry)
        {
            Contract.Requires(countRetry>0);
            Contract.Requires(periodRetry!=null);
            CountRetry = countRetry;
            PeriodRetry = periodRetry;
            IsForceStart = false;
        }

        /// <summary>
        /// Only for TimeoutRestore
        /// </summary>
        /// <param name="countRetry"></param>
        /// <param name="periodRetry"></param>
        /// <param name="isForceStart"></param>
        /// <param name="deleteTimeout"></param>
        public RestoreModuleConfiguration(int countRetry, TimeSpan periodRetry, bool isForceStart,
            TimeSpan deleteTimeout)
        {
            Contract.Requires(countRetry > 0);
            Contract.Requires(periodRetry != null);
            Contract.Requires(deleteTimeout != null);
            CountRetry = countRetry;
            PeriodRetry = periodRetry;
            IsForceStart = isForceStart;
            DeleteTimeout = deleteTimeout;
        }
    }
}

using System;
using System.Diagnostics.Contracts;

namespace Qoollo.Impl.Configurations
{
    internal class ProxyCacheConfiguration
    {
        public TimeSpan TimeAliveSec { get; private set; }

        public ProxyCacheConfiguration(TimeSpan timeAliveSec)
        {
            Contract.Requires(timeAliveSec != null);
            TimeAliveSec = timeAliveSec;
        }
    }
}

using System;
using System.Diagnostics.Contracts;

namespace Qoollo.Impl.Configurations
{
    internal class DistributorCacheConfiguration
    {
        public TimeSpan TimeAliveBeforeDeleteMls { get; private set; }
        public TimeSpan TimeAliveAfterUpdateMls { get; private set; }

        public DistributorCacheConfiguration(TimeSpan timeAliveBeforeDeleteMls, TimeSpan timeAliveAfterUpdateMls)
        {
            Contract.Requires(timeAliveAfterUpdateMls!=null);
            Contract.Requires(timeAliveBeforeDeleteMls !=null);
            TimeAliveAfterUpdateMls = timeAliveAfterUpdateMls;
            TimeAliveBeforeDeleteMls = timeAliveBeforeDeleteMls;
        }
    }
}

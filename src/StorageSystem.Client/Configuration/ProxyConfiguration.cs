using System;
using System.Diagnostics.Contracts;
using Qoollo.Client.Support;

namespace Qoollo.Client.Configuration
{
    public class ProxyConfiguration
    {
        /// <summary>
        /// Time how to store information about the change of distributor.
        /// (If )
        /// </summary>
        public TimeSpan ChangeDistributorTimeoutSec { get; private set; }
        /// <summary>
        /// Timeout for sync operations
        /// </summary>
        public TimeSpan SyncOperationsTimeoutSec { get; private set; }
        /// <summary>
        /// Async update information from remote servers        
        /// </summary>
        public TimeSpan AsyncUpdateTimeout { get; private set; }
        /// <summary>
        /// Server ping period
        /// </summary>
        public TimeSpan AsyncPingTimeout { get; private set; }

        public ProxyConfiguration(TimeSpan changeDistributorTimeoutSec, TimeSpan syncOperationsTimeoutSec,
            TimeSpan asyncUpdateTimeout, TimeSpan asyncPingTimeout)
        {
            Contract.Requires(changeDistributorTimeoutSec!=null);
            Contract.Requires(syncOperationsTimeoutSec != null);
            Contract.Requires(asyncPingTimeout != null);
            Contract.Requires(asyncUpdateTimeout != null);

            AsyncPingTimeout = asyncPingTimeout;
            AsyncUpdateTimeout = asyncUpdateTimeout;
            SyncOperationsTimeoutSec = syncOperationsTimeoutSec;
            ChangeDistributorTimeoutSec = changeDistributorTimeoutSec;
        }

        public ProxyConfiguration(TimeSpan changeDistributorTimeoutSec, TimeSpan syncOperationsTimeoutSec,
            TimeSpan asyncUpdateTimeout):this(changeDistributorTimeoutSec, syncOperationsTimeoutSec,
            asyncUpdateTimeout, Consts.AsyncPingTimeout)
        {
        }

        public ProxyConfiguration(TimeSpan changeDistributorTimeoutSec, TimeSpan syncOperationsTimeoutSec)
            : this(changeDistributorTimeoutSec, syncOperationsTimeoutSec,
                Consts.AsyncUpdateTimeout, Consts.AsyncPingTimeout)
        {
        }

        public ProxyConfiguration(TimeSpan changeDistributorTimeoutSec)
            : this(changeDistributorTimeoutSec, Consts.SyncOperationsTimeoutSec, Consts.AsyncUpdateTimeout,
                Consts.AsyncPingTimeout)
        {
        }

        public ProxyConfiguration()
            : this(Consts.ChangeDistributorTimeoutSec, Consts.SyncOperationsTimeoutSec, Consts.AsyncUpdateTimeout,
                Consts.AsyncPingTimeout)
        {
        }
    }
}

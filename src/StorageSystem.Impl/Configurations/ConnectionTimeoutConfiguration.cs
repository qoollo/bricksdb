using System;
using System.Diagnostics.Contracts;

namespace Qoollo.Impl.Configurations
{
    internal class ConnectionTimeoutConfiguration
    {
        public ConnectionTimeoutConfiguration(TimeSpan openTimeout, TimeSpan sendTimeout)
        {
            Contract.Requires(openTimeout!=null);
            Contract.Requires(sendTimeout != null);
            SendTimeout = sendTimeout;
            OpenTimeout = openTimeout;
        }

        public TimeSpan SendTimeout { get; private set; }
        public TimeSpan OpenTimeout { get; private set; }
    }
}

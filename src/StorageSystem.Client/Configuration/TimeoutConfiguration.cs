using System;
using System.Diagnostics.Contracts;
using Qoollo.Client.Support;

namespace Qoollo.Client.Configuration
{
    public class TimeoutConfiguration
    {
        public TimeSpan SendTimeout { get; private set; }
        public TimeSpan OpenTimeout { get; private set; }

        public TimeoutConfiguration(TimeSpan openTimeout, TimeSpan sendTimeout)
        {
            Contract.Requires(openTimeout!=null);
            Contract.Requires(sendTimeout != null);
            SendTimeout = sendTimeout;
            OpenTimeout = openTimeout;
        }

        public TimeoutConfiguration() : this(Consts.OpenTimeout, Consts.SendTimeout)
        {
        }
    }
}

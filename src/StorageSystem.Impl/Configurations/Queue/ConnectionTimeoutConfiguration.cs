using System;
using System.Diagnostics.Contracts;

namespace Qoollo.Impl.Configurations.Queue
{
    public class ConnectionTimeoutConfiguration
    {
        public int SendTimeoutMls { get; protected set; }
        public int OpenTimeoutMls { get; protected set; }
    }
}

using System.Diagnostics.Contracts;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Configurations.Queue
{
    public class NetConfiguration
    {
        public int Port { get; protected set; }
        public string Host { get; protected set; }

        internal string ServiceName { get; set; }
        internal ServerId ServerId { get; set; }

        public NetConfiguration()
        {
        }

        internal NetConfiguration(int port, string host = "localhost")
        {
            Port = port;
            Host = host;
        }
    }
}

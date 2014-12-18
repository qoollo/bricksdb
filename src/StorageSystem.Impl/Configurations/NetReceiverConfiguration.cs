using System.Diagnostics.Contracts;

namespace Qoollo.Impl.Configurations
{
    internal class NetReceiverConfiguration
    {
        public int Port { get; private set; }
        public string Host { get; private set; }
        public string Service { get; private set; }

        public NetReceiverConfiguration(int port, string host, string service)
        {
            Contract.Requires(port>0);
            Contract.Requires(host!="");
            Contract.Requires(service!="");
            Port = port;
            Host = host;
            Service = service;
        }
    }
}

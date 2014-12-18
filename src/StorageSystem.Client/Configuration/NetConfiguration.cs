using System.Diagnostics.Contracts;
using Qoollo.Client.Support;

namespace Qoollo.Client.Configuration
{
    public class NetConfiguration
    {
        /// <summary>
        /// Host for remote connection for this server
        /// </summary>
        public string Host { get; private set; }
        /// <summary>
        /// Port for remote connection for this server        
        /// </summary>
        public int Port { get; private set; }
        /// <summary>
        /// Wcf name for service
        /// </summary>
        public string WcfServiceName { get; private set; }
        /// <summary>
        /// Size of connection pool for each server
        /// </summary>
        public int CountConnectionsToSingleServer { get; private set; }

        public NetConfiguration(string host, int port, string wcfServiceName,
            int countConnectionsToSingleServer)
        {
            Contract.Requires(host != "");
            Contract.Requires(wcfServiceName != "");
            Contract.Requires(port!=0);
            Contract.Requires(countConnectionsToSingleServer != 0);

            Host = host;
            Port = port;
            WcfServiceName = wcfServiceName;
            CountConnectionsToSingleServer = countConnectionsToSingleServer;
        }

        public NetConfiguration(string host, int port, string wcfServiceName)
            :this(host, port, wcfServiceName, Consts.CountConnectionsToSingleServer)
        {
        }

        public NetConfiguration(string host, int port)
            : this(host, port, Consts.WcfServiceName, Consts.CountConnectionsToSingleServer)
        {
        }
    }
}

using System.Diagnostics.Contracts;
using Qoollo.Client.Support;

namespace Qoollo.Client.Configuration
{
    public class DistributorNetConfiguration
    {
        /// <summary>
        /// Host for remote connection for this server
        /// </summary>
        public string Host { get; private set; }

        /// <summary>
        /// The port on which the Proxy is connected to this server
        /// </summary>
        public int PortForProxy { get; private set; }

        /// <summary>
        /// The port on which the Writer is connected to this server
        /// </summary>
        public int PortForStorage { get; private set; }

        /// <summary>
        /// Wcf name for service
        /// </summary>
        public string WcfServiceName { get; private set; }

        /// <summary>
        /// Size of connection pool for each server
        /// </summary>
        public int CountConnectionsToSingleServer { get; private set; }

        public DistributorNetConfiguration(string host, int portForProxy, int portForStorage,
            string wcfServiceName, int countConnectionsToSingleServer)
        {
            Contract.Requires(host != "");
            Contract.Requires(wcfServiceName != "");
            Contract.Requires(portForProxy != 0);
            Contract.Requires(portForStorage != 0);
            Contract.Requires(countConnectionsToSingleServer != 0);


            PortForStorage = portForStorage;
            Host = host;
            PortForProxy = portForProxy;
            WcfServiceName = wcfServiceName;
            CountConnectionsToSingleServer = countConnectionsToSingleServer;
        }

        public DistributorNetConfiguration(string host, int portForProxy, int portForStorage,
            string wcfServiceName) :
                this(host, portForProxy, portForStorage, wcfServiceName, Consts.CountConnectionsToSingleServer)
        {

        }

        public DistributorNetConfiguration(string host, int portForProxy, int portForStorage) :
            this(host, portForProxy, portForStorage, Consts.WcfServiceName, Consts.CountConnectionsToSingleServer)
        {

        }
    }
}

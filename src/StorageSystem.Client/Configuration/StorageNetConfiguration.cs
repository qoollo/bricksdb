using System.Diagnostics.Contracts;
using Qoollo.Client.Support;

namespace Qoollo.Client.Configuration
{
    public class StorageNetConfiguration
    {
        /// <summary>
        /// Host for remote connection for this server
        /// </summary>
        public string Host { get; private set; }
        /// <summary>
        /// The port on which the Distributor is connected to this server
        /// </summary>
        public int PortForDitributor { get; private set; }
        /// <summary>
        /// The port on which the Collector is connected to this server
        /// </summary>
        public int PortForCollector { get; private set; }
        /// <summary>
        /// Wcf name for service
        /// </summary>
        public string WcfServiceName { get; private set; }
        /// <summary>
        /// Size of connection pool for each server
        /// </summary>
        public int CountConnectionsToSingleServer { get; private set; }

        public int TrimPeriod { get; private set; }

        public StorageNetConfiguration( string host, int portForDitributor, int portForCollector,
            string wcfServiceName, int countConnectionsToSingleServer, int trimPeriod)
        {
            Contract.Requires(wcfServiceName != "");
            Contract.Requires(host != "");
            Contract.Requires(portForDitributor != 0);
            Contract.Requires(portForCollector != 0);
            Contract.Requires(countConnectionsToSingleServer > 0);
            Contract.Requires(trimPeriod > 0);

            PortForCollector = portForCollector;
            Host = host;            
            PortForDitributor = portForDitributor;
            WcfServiceName = wcfServiceName;
            CountConnectionsToSingleServer = countConnectionsToSingleServer;
            TrimPeriod = trimPeriod;
        }

        public StorageNetConfiguration(string host, int portForDitributor, int portForCollector,
            string wcfServiceName, int countConnectionsToSingleServer)
            : this(host, portForDitributor, portForCollector, wcfServiceName, countConnectionsToSingleServer, Consts.TrimPeriod)
        {
        }

        public StorageNetConfiguration(string host, int portForDitributor, int portForCollector,
            string wcfServiceName)
            : this(host, portForDitributor, portForCollector, wcfServiceName, Consts.CountConnectionsToSingleServer, Consts.TrimPeriod)
        {
        }

        public StorageNetConfiguration(string host, int portForDitributor, int portForCollector)
            : this(
                host, portForDitributor, portForCollector, Consts.WcfServiceName, Consts.CountConnectionsToSingleServer, Consts.TrimPeriod)
        {
        }
    }
}

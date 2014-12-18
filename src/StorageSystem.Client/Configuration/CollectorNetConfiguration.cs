using System.Diagnostics.Contracts;
using Qoollo.Client.Support;

namespace Qoollo.Client.Configuration
{
    public class CollectorNetConfiguration
    {
        public CollectorNetConfiguration(string wcfServiceName, int countConnectionsToSingleServer)
        {
            Contract.Requires(wcfServiceName!="");
            Contract.Requires(countConnectionsToSingleServer>0);
            CountConnectionsToSingleServer = countConnectionsToSingleServer;
            WcfServiceName = wcfServiceName;
        }

        public CollectorNetConfiguration(string wcfServiceName)
            :this(wcfServiceName, Consts.CountConnectionsToSingleServer)
        {            
        }

        public CollectorNetConfiguration()
            : this(Consts.WcfServiceName, Consts.CountConnectionsToSingleServer)
        {
        }

        /// <summary>
        /// Wcf name for service
        /// </summary>
        public string WcfServiceName { get; private set; }
        /// <summary>
        /// Size of connection pool for each server
        /// </summary>
        public int CountConnectionsToSingleServer { get; private set; }
    }
}

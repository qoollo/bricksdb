using System.Diagnostics.Contracts;
using Qoollo.Client.Support;

namespace Qoollo.Client.Configuration
{
    public class CollectorNetConfiguration
    {
        /// <summary>
        /// Wcf name for service
        /// </summary>
        public string WcfServiceName { get; private set; }
        /// <summary>
        /// Size of connection pool for each server
        /// </summary>
        public int CountConnectionsToSingleServer { get; private set; }

        public int TrimPeriod { get; private set; }
        public CollectorNetConfiguration(string wcfServiceName, int countConnectionsToSingleServer, int trimPeriod)
        {
            Contract.Requires(wcfServiceName!="");
            Contract.Requires(countConnectionsToSingleServer>0);
            Contract.Requires(trimPeriod>0);
            TrimPeriod = trimPeriod;
            CountConnectionsToSingleServer = countConnectionsToSingleServer;
            WcfServiceName = wcfServiceName;
        }

        public CollectorNetConfiguration(string wcfServiceName, int countConnectionsToSingleServer)
            : this(wcfServiceName, countConnectionsToSingleServer, Consts.TrimPeriod)
        {
        }

        public CollectorNetConfiguration(string wcfServiceName)
            :this(wcfServiceName, Consts.CountConnectionsToSingleServer, Consts.TrimPeriod)
        {            
        }

        public CollectorNetConfiguration()
            : this(Consts.WcfServiceName, Consts.CountConnectionsToSingleServer, Consts.TrimPeriod)
        {
        }        
    }
}

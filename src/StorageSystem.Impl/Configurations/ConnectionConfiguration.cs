using System.Diagnostics.Contracts;

namespace Qoollo.Impl.Configurations
{
    internal class ConnectionConfiguration
    {        
        public string ServiceName { get; private set; }
        public int MaxElementCount { get; private set; }
        public int TrimPeriod { get; private set; }

        public ConnectionConfiguration(string serviceName, int maxElementCount, int trimPeriod = 10000)
        {            
            Contract.Requires(serviceName!="");
            Contract.Requires(maxElementCount>1);
            Contract.Requires(trimPeriod > 0);

            ServiceName = serviceName;
            MaxElementCount = maxElementCount;
            TrimPeriod = trimPeriod;
        }
    }
}

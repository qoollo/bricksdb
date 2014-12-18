using System.Diagnostics.Contracts;

namespace Qoollo.Impl.Configurations
{
    internal class ConnectionConfiguration
    {
        public string ServiceName { get; private set; }
        public int MaxElementCount { get; private set; }

        public ConnectionConfiguration(string serviceName, int maxElementCount)
        {
            Contract.Requires(serviceName!="");
            Contract.Requires(maxElementCount>1);

            ServiceName = serviceName;
            MaxElementCount = maxElementCount;
        }
    }
}

using System.Diagnostics.Contracts;

namespace Qoollo.Impl.Configurations
{
    internal class DistributorHashConfiguration
    {
        public int CountReplics { get; private set; }

        public DistributorHashConfiguration(int countReplics)
        {
            Contract.Requires(countReplics>0);
            CountReplics = countReplics;
        }
    }
}

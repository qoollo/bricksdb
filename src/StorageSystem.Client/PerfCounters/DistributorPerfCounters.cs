using Libs.PerformanceCounters;
using Qoollo.Impl.DistributorModules.PerfCounters;

namespace Qoollo.Client.PerfCounters
{
    [PerfCountersContainer]
    public class DistributorPerfCounters
    {
        [PerfCountersInitializationMethod]
        public static void Init(CategoryWrapper root)
        {
            DistributorCounters.Init(root);            
        }
    }
}

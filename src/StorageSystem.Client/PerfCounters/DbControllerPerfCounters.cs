using Libs.PerformanceCounters;
using Qoollo.Impl.DbController.PerfCounters;

namespace Qoollo.Client.PerfCounters
{
    [PerfCountersContainer]
    public class DbControllerPerfCounters
    {
        [PerfCountersInitializationMethod]
        public static void Init(CategoryWrapper root)
        {
            DbControllerCounters.Init(root);            
        }
    }
}

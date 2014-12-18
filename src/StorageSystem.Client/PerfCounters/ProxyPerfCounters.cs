using Libs.PerformanceCounters;
using Qoollo.Impl.Proxy.PerfCounters;

namespace Qoollo.Client.PerfCounters
{
    [PerfCountersContainer]
    public class ProxyPerfCounters
    {
        [PerfCountersInitializationMethod]
        public static void Init(CategoryWrapper root)
        {
            ProxyCounters.Init(root);
        }
    }
}

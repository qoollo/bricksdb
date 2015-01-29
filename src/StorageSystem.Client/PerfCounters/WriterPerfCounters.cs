using Qoollo.Impl.Writer.PerfCounters;
using Qoollo.PerformanceCounters;

namespace Qoollo.Client.PerfCounters
{
    [PerfCountersContainer]
    public class WriterPerfCounters
    {
        [PerfCountersInitializationMethod]
        public static void Init(CategoryWrapper root)
        {
            WriterCounters.Init(root);            
        }
    }
}

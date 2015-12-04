using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Client.Profiler
{
    public static class PoolsProfiler
    {
        public static void SetProfiler(Qoollo.Logger.Logger logger)
        {
            Qoollo.Impl.PerfCounters.PoolsProfiler.SetProfiler(logger);
        }
    }
}

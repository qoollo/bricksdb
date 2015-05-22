using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Qoollo.Benchmark.Statistics
{
    class BenchmarkMetrics
    {
        public BenchmarkMetrics()
        {
            _metrics = new List<SingleMetric>();
            _timer = new TimerStat(_metrics);
            _timer.Start();
        }

        private readonly List<SingleMetric> _metrics;
        private readonly TimerStat _timer;

        public void AddMetrics(SingleMetric metric)
        {
            _metrics.Add(metric);
        }

        public static AvgMetric CreateAvgMetric(string name)
        {
            return new AvgMetric(name);
        }

        public AvgMetric GetAvgMetric(string name)
        {
            var metric = _metrics.FirstOrDefault(x => string.Equals(name, x.Name)) ?? CreateAvgMetric(name);
            return metric as AvgMetric;
        }

        public void CreateStatistics()
        {
            _timer.Stop();
        }

        
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Benchmark.Statistics
{
    class MetricsCollection:SingleMetric
    {
        private readonly Dictionary<string, SingleMetric> _metrics;
 
        public MetricsCollection(string name) : base(name)
        {
            _metrics = new Dictionary<string, SingleMetric>();
        }

        public void AddMetrics(SingleMetric metric)
        {
            _metrics.Add(metric.Name, metric);
        }

        public SingleMetric Get(string name)
        {
            return _metrics[name];
        }

        public override void Tick()
        {
            foreach (var metric in _metrics.Values)
            {
                metric.Tick();
            }
        }

        public override string TotalStatistics()
        {
            return _metrics.Aggregate("", (current, metric) => current + metric.Value.TotalStatistics());
        }

        public override string ToString()
        {
            return _metrics.Aggregate("", (current, metric) => current + metric);
        }
    }
}

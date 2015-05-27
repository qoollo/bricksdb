using System;
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

        private void AddMetrics(SingleMetric metric)
        {
            _metrics.Add(metric);
        }

        public static AvgMetric CreateAvgMetric(string name)
        {
            return new AvgMetric(name);
        }

        public static MetricsCollection CreateMetricsCollection(string name)
        {
            return new MetricsCollection(name);
        }

        public AvgMetric GetAvgMetric(string name)
        {
            var metric = _metrics.FirstOrDefault(x => string.Equals(name, x.Name));
            if (metric == null)
            {
                metric = CreateAvgMetric(name);
                AddMetrics(metric);
            }

            return metric as AvgMetric;
        }

        public MetricsCollection GetMetricsCollection(string name)
        {
            var metric = _metrics.FirstOrDefault(x => string.Equals(name, x.Name));
            if (metric == null)
            {
                metric = CreateMetricsCollection(name);
                AddMetrics(metric);
            }

            return metric as MetricsCollection;
        }

        public void CreateStatistics()
        {
            _timer.Stop();
            _timer.TimerTick();
            
            PrintTotalStatistics();
        }

        private void PrintTotalStatistics()
        {
            Console.CursorTop += _metrics.Count;
            Console.WriteLine();

            foreach (var metric in _metrics)
            {                                
                Console.WriteLine(metric.TotalStatistics());
                Console.WriteLine("-------------------");
            }
        }
    }
}

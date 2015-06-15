using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Qoollo.Benchmark.csv;

namespace Qoollo.Benchmark.Statistics
{
    class BenchmarkMetrics
    {
        public BenchmarkMetrics()
        {
            _metrics = new List<SingleMetric>();
            _timer = new TimerStat(_metrics);            
        }

        private readonly List<SingleMetric> _metrics;
        private readonly TimerStat _timer;        

        private void AddMetrics(SingleMetric metric)
        {
            _metrics.Add(metric);
        }

        public void AddCsvFileProcessor(CsvFileProcessor csvFileProcessor)
        {
            _timer.AddCsvFileProcessor(csvFileProcessor);
            _metrics.ForEach(x=>x.SetCsvFileProcessor(csvFileProcessor));
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
            Stop();
            
            PrintTotalStatistics();
        }

        public void Stop()
        {
            _timer.TimerTick();
            _timer.Stop();     
        }

        public void Start()
        {
            _timer.Start();
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

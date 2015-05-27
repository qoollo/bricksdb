using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Benchmark.Send;
using Qoollo.Benchmark.Statistics;

namespace Qoollo.Benchmark.Load
{
    internal class GetLoadTest : LoadTest
    {
        public GetLoadTest(DbWriterAdapter adapter, KeyGenerator keyGenerator)
            : base(adapter)
        {
            Contract.Requires(keyGenerator != null);
            Contract.Requires(adapter != null);
            _adapter = adapter;
            _keyGenerator = keyGenerator;
        }

        private readonly DbWriterAdapter _adapter;
        private readonly KeyGenerator _keyGenerator;
        private AvgMetric _metric;

        public override bool OneDataProcess()
        {
            var key = _keyGenerator.Generate();
            var timer = _metric.StartMeasure();

            _metric.AddResult(_adapter.Read(key));
            _metric.StopMeasure(timer);

            return true;
        }

        public override void CreateMetric(BenchmarkMetrics metrics)
        {
            _metric = metrics.GetAvgMetric("GET");
        }

        public override SingleMetric GetMetric()
        {
            return _metric;
        }
    }
}

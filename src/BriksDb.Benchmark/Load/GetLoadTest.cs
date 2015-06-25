using System.Diagnostics.Contracts;
using Qoollo.Benchmark.Send.Interfaces;
using Qoollo.Benchmark.Statistics;

namespace Qoollo.Benchmark.Load
{
    internal class GetLoadTest : LoadTest
    {
        public GetLoadTest(ICrud adapter, KeyGenerator keyGenerator)
            : base(adapter)
        {
            Contract.Requires(keyGenerator != null);
            Contract.Requires(adapter != null);
            _adapter = adapter;
            _keyGenerator = keyGenerator;
        }

        private readonly ICrud _adapter;
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
    }
}

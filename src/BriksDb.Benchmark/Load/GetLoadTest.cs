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
    class GetLoadTest : LoadTest
    {
        public GetLoadTest(Func<DataSender> senderFactory, KeyGenerator keyGenerator)
            : base(senderFactory())
        {
            Contract.Requires(keyGenerator != null);

            _keyGenerator = keyGenerator;
        }

        private readonly KeyGenerator _keyGenerator;
        private AvgMetric _metric;        

        public override void OneDataProcess()
        {
            var key = _keyGenerator.Generate();
            var timer = _metric.StartMeasure();

            _metric.AddResult(Sender.Read(key));
            _metric.StopMeasure(timer);
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

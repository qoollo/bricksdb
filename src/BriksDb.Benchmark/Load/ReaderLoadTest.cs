using System;
using System.Diagnostics.Contracts;
using Qoollo.Benchmark.Send;
using Qoollo.Benchmark.Statistics;
using Qoollo.Client.CollectorGate;

namespace Qoollo.Benchmark.Load
{
    class ReaderLoadTest:LoadTest
    {
        public class MetricName
        {
            public const string Total = "Total";
            public const string Single = "Single";
        }

        public ReaderLoadTest(ReaderAdapter adapter, BlockingQueue<QueryDescription> queries)
            : base(adapter)
        {
            Contract.Requires(queries != null);
            _adapter = adapter;
            _queries = queries;
        }

        private readonly ReaderAdapter _adapter;
        private readonly BlockingQueue<QueryDescription> _queries;
        private StorageDbReader _reader;
        private MetricsCollection _metrics;

        private bool ReadData()
        {
            var timer = _metrics.Get(MetricName.Single).StartMeasure();
            var ret = _reader.NextResult();
            _metrics.Get(MetricName.Single).StopMeasure(timer);
            _metrics.Get(MetricName.Single).AddResult(true);
            return ret;
        }

        public override bool OneDataProcess()
        {
            var exit = true;
            if (_reader == null || !ReadData())
            {
                if (CreateNewReader())
                    ReadData();
                else
                    exit = false;
            }
            return exit;
        }

        public bool NeedCreateReader()
        {
            return _reader == null || !ReadData();
        }

        public bool CreateNewReader()
        {
            var value = _queries.Dequeue();
            _reader = value.IsValueExist ? _adapter.ExecuteQuery(value.Value) : null;
            return _reader != null;
        }

        public override void CreateMetric(BenchmarkMetrics metrics)
        {
            _metrics = metrics.GetMetricsCollection("COLLECTOR");
            _metrics.AddMetrics(new AvgMetric(MetricName.Total));
            _metrics.AddMetrics(new AvgMetric(MetricName.Single));
        }

        public override SingleMetric GetMetric()
        {
            return _metrics;
        }
    }
}

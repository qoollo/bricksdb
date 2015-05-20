using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Qoollo.Benchmark.Send;
using Qoollo.Benchmark.Statistics;

namespace Qoollo.Benchmark.Load
{
    class SetLoadTest : LoadTest
    {
        public SetLoadTest(DataSender sender, Func<IDataGenerator> dataGeneratorFactory,
            Func<KeyGenerator> keyGeneratorFactory)
            :base(sender)
        {
            Contract.Requires(sender != null);
            Contract.Requires(dataGeneratorFactory != null);
            Contract.Requires(keyGeneratorFactory != null);

            _dataGenerator = dataGeneratorFactory();
            _keyGenerator = keyGeneratorFactory();
        }

        private readonly IDataGenerator _dataGenerator;
        private readonly KeyGenerator _keyGenerator;
        private AvgMetric _metric;        
        private const int GenerateCount = 100;
        
        private IEnumerator<string> _iterator;

        public override void OneDataProcess()
        {
            if (_iterator == null || !_iterator.MoveNext())
                GenerateNextData();

            SendData();
        }

        private void SendData()
        {
            var key = _keyGenerator.Generate();
            var timer = _metric.StartMeasure();

            _metric.AddResult(Sender.Send(key, _iterator.Current));
            _metric.StopMeasure(timer);
        }

        private void GenerateNextData(int count = GenerateCount)
        {             
             _iterator = _dataGenerator.GenerateData(count).GetEnumerator();
        }

        public override void CreateMetric(BenchmarkMetrics metrics)
        {
            _metric = metrics.GetAvgMetric("SET");
        }

        public override SingleMetric GetMetric()
        {
            return _metric;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Qoollo.Benchmark.DataGenerator;
using Qoollo.Benchmark.Send;
using Qoollo.Benchmark.Statistics;

namespace Qoollo.Benchmark.Load
{
    class SetLoadTest : LoadTest
    {
        public SetLoadTest(DbWriterAdapter adapter, IDataGenerator dataGenerator, KeyGenerator keyGenerator)
            : base(adapter)
        {            
            Contract.Requires(dataGenerator != null);
            Contract.Requires(keyGenerator != null);
            Contract.Requires(adapter != null);

            _adapter = adapter;
            _dataGenerator = dataGenerator;
            _keyGenerator = keyGenerator;
        }

        private readonly DbWriterAdapter _adapter;
        private readonly IDataGenerator _dataGenerator;
        private readonly KeyGenerator _keyGenerator;
        private AvgMetric _metric;        
        private const int GenerateCount = 100;
        
        private IEnumerator<string> _iterator;

        public override bool OneDataProcess()
        {
            if (_iterator == null || !_iterator.MoveNext())
                GenerateNextData();

            SendData();
            return true;
        }

        private void SendData()
        {
            var key = _keyGenerator.Generate();
            var timer = _metric.StartMeasure();
            
            _metric.AddResult(_adapter.Send(key, _iterator.Current));
            _metric.StopMeasure(timer);
        }

        private void GenerateNextData(int count = GenerateCount)
        {             
             _iterator = _dataGenerator.GenerateData(count).GetEnumerator();
            _iterator.MoveNext();
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
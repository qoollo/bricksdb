using System;
using System.Collections.Generic;
using System.Linq;

namespace Qoollo.Benchmark.Statistics
{
    internal class AvgMetric : SingleMetric
    {
        public AvgMetric(string name) : base(name)
        {
            _values = new List<int>();
            _avgPerSec = 0;
            _lastCount = 0;
            _maxPerSec = 0;
        }

        private readonly List<int> _values;
        private int _lastCount;
        private const int AvgCount = 10;
        private int _avgPerSec;
        private int _maxPerSec;

        public override void Tick()
        {
            _values.Add(TotalCount - _lastCount);            

            if (_values.Count > AvgCount)
                _values.RemoveAt(0);
            _lastCount = TotalCount;

            _avgPerSec = _values.Sum()/_values.Count;
            _maxPerSec = Math.Max(_maxPerSec, _avgPerSec);
        }

        public override string TotalStatistics()
        {
            return string.Format("{0}\nMax PerSec: {1}", base.TotalStatistics(), _maxPerSec);
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}, total: {2}", Name, _avgPerSec, TotalCount);
        }
    }
}

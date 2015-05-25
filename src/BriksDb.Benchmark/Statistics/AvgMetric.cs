using System.Collections.Generic;
using System.Linq;

namespace Qoollo.Benchmark.Statistics
{
    internal class AvgMetric : SingleMetric
    {
        public int AvgPerSec { get; private set; }

        public AvgMetric(string name) : base(name)
        {
            _values = new List<int>();
            AvgPerSec = 0;
            _lastCount = 0;
        }

        private readonly List<int> _values;
        private int _lastCount;
        private const int AvgCount = 10;

        public override void Tick()
        {
            _values.Add(TotalCount - _lastCount);
            if (_values.Count > AvgCount)
                _values.RemoveAt(0);
            _lastCount = TotalCount;

            AvgPerSec = _values.Sum()/_values.Count;
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}, total: {2}, fail: {3}", Name, AvgPerSec, TotalCount, FailCount);
        }
    }
}

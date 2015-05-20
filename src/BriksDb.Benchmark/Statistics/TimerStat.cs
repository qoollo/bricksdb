using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;

namespace Qoollo.Benchmark.Statistics
{
    class TimerStat
    {
        public TimerStat(IEnumerable<SingleMetric> metrics)
        {
            Contract.Requires(metrics != null);
            _metrics = metrics;
        }

        private const int TimerTickMls = 1000;
        private readonly IEnumerable<SingleMetric> _metrics;
        private Timer _timer;

        public void Start()
        {
            _timer = new Timer(TimerTick, null, 0, TimerTickMls);            
        }

        private void TimerTick(object state)
        {
            foreach (var metric in _metrics)
            {
                metric.Tick();
                Console.WriteLine(metric);
            }
        }

        public void Stop()
        {
            if (_timer != null)
                _timer.Dispose();
        }
    }
}
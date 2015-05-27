using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;

namespace Qoollo.Benchmark.Statistics
{
    class TimerStat
    {
        class ConsoleCoordinate:IDisposable
        {
            public ConsoleCoordinate()
            {                
                _x = Console.CursorLeft;
                _y = Console.CursorTop;
            }

            private readonly int _x;
            private readonly int _y;

            public void Dispose()
            {
                Console.CursorLeft = _x;
                Console.CursorTop = _y;
            }
        }

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
            TickAllMetrics();            
            PrintCurrentInfo();
        }        

        private void TickAllMetrics()
        {
            foreach (var singleMetric in _metrics)
            {
                singleMetric.Tick();
            }
        }

        private void PrintCurrentInfo()
        {
            using (new ConsoleCoordinate())
            {
                foreach (var metric in _metrics)
                {                    
                    Console.WriteLine(metric);
                }
            }
        }
        
        public void TimerTick()
        {
            TimerTick(null);
        }

        public void Stop()
        {
            if (_timer != null)
                _timer.Dispose();
        }
        
    }
}
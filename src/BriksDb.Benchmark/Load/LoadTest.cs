﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Qoollo.Benchmark.Send;
using Qoollo.Benchmark.Statistics;
using Qoollo.Concierge.Attributes;

namespace Qoollo.Benchmark.Load
{
    abstract class LoadTest
    {
        protected LoadTest(IDataAdapter adapter)
        {
            Contract.Requires(adapter!=null);            
            adapter.Start();
        }

        public abstract bool OneDataProcess();        
        public abstract void CreateMetric(BenchmarkMetrics metrics);
        public abstract SingleMetric GetMetric();        
    }
}

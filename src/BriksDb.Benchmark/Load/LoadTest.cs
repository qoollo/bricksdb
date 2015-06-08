using System.Diagnostics.Contracts;
using Qoollo.Benchmark.Send;
using Qoollo.Benchmark.Send.Interfaces;
using Qoollo.Benchmark.Statistics;

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

    }
}

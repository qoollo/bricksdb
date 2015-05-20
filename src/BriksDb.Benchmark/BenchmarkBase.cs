using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Threading;
using System.Threading.Tasks;
using Qoollo.Benchmark.Load;
using Qoollo.Benchmark.Statistics;

namespace Qoollo.Benchmark
{
    internal class BenchmarkTest
    {        
        public BenchmarkTest(int countThreads, int countData = -1)
        {
            Contract.Requires(countThreads > 0);
            Contract.Requires(countData == -1 || countData > 0);
            _countThreads = countThreads;
            _countData = countData;
            _testFactory = new List<Func<LoadTest>>();
            _token = new CancellationTokenSource();
        }

        private readonly int _countThreads;
        private readonly int _countData;
        private readonly List<Func<LoadTest>> _testFactory;
        private readonly CancellationTokenSource _token;

        private void ThreadTest(List<LoadTest> tests, int countData)
        {
            var current = 0;
            while (!_token.IsCancellationRequested)
            {
                foreach (var loadTest in tests)
                {
                    if (current++ >= countData)
                        break;

                    loadTest.OneDataProcess();
                }
            }
        }

        private void RunAsync()
        {
            try
            {
                var metric = new BenchmarkMetrics();
                var taskList = new List<Task>();

                var restCount = _countData;

                for (int i = 0; i < _countThreads; i++)
                {
                    var count = _countData/_countThreads;
                    restCount -= count;

                    if (i == _countThreads - 1 || restCount != 0)
                        count += restCount;

                    taskList.Add(CreateThread(metric, count));
                }

                Task.WaitAll(taskList.ToArray(), _token.Token);

                metric.CreateStatistics();
            }
            catch (OperationCanceledException)
            {
            }
        }

        private Task CreateThread(BenchmarkMetrics metrics, int count)
        {
            var tests = _testFactory.Select(x => x()).ToList();
            tests.ForEach(x => CreateMetrics(x, metrics));
            

            return Task.Factory.StartNew(() => ThreadTest(tests, count));
        }

        private void CreateMetrics(LoadTest test, BenchmarkMetrics metrics)
        {
            test.CreateMetric(metrics);
            metrics.AddMetrics(test.GetMetric());
        }

        public void Run()
        {
            Task.Factory.StartNew(RunAsync);
        }

        public void Cancel()
        {
            _token.Cancel();
        }
    }
}

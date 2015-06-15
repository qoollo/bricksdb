using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Qoollo.Benchmark.csv;
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

        public BenchmarkTest AddLoadTestFactory(Func<LoadTest> factory)
        {
            _testFactory.Add(factory);
            return this;
        }       

        private void RunAsync(CsvFileProcessor processor)
        {
            var metric = new BenchmarkMetrics();

            try
            {
                var taskList = new List<Task>();
                var count = _countData == -1 ? _countData : _countData/_countThreads;

                for (int i = 0; i < _countThreads; i++)
                {
                    if (_countData != -1 && i == _countThreads - 1 && _countData%_countThreads != 0)
                        count += _countData%_countThreads;

                    taskList.Add(CreateThread(metric, count, processor));
                }

                metric.Start();
                Task.WaitAll(taskList.ToArray(), _token.Token);

                metric.CreateStatistics();
            }

            catch (OperationCanceledException)
            {
            }
            catch (Exception)
            {
                metric.Stop();
                throw;
            }
        }

        public void Run(CsvFileProcessor processor = null)
        {
            RunAsync(processor);
        }

        public void Cancel()
        {
            _token.Cancel();
        }

        private Task CreateThread(BenchmarkMetrics metrics, int count, CsvFileProcessor processor)
        {
            var tests = CreateMetrics(metrics, processor);
            metrics.AddCsvFileProcessor(processor);

            return Task.Factory.StartNew(() => ThreadTest(tests, count));
        }

        private void ThreadTest(List<LoadTest> tests, int countData)
        {
            var current = 0;
            bool exit = true;
            while (!_token.IsCancellationRequested && exit)
            {                
                foreach (var loadTest in tests)
                {
                    if (current++ >= countData && countData != -1 || !exit)
                    {
                        exit = false;
                        break;
                    }
                    exit = loadTest.OneDataProcess();
                }
            }            
        }

        private List<LoadTest> CreateMetrics(BenchmarkMetrics metrics, CsvFileProcessor processor)
        {
            var tests = _testFactory.Select(x => x()).ToList();
            tests.ForEach(x => x.CreateMetric(metrics));
            
            return tests;
        }
    }
}

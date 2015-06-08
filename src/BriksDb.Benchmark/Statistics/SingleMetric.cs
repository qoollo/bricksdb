using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using Qoollo.Benchmark.csv;

namespace Qoollo.Benchmark.Statistics
{
    internal abstract class SingleMetric
    {
        public string Name
        {
            get { return _name; }
        }

        public int TotalCount
        {
            get { return _totalCount; }
        }

        public int FailCount
        {
            get { return _failCount; }
        }

        protected SingleMetric(string name)
        {
            Contract.Requires(!string.IsNullOrEmpty(name));
            _name = name;
            _totalCount = 0;
            _failCount = 0;
            _operationTime = new List<long>();
        }

        private readonly string _name;
        private int _totalCount;
        private readonly List<long> _operationTime;
        private int _failCount;
        private CsvFileProcessor _csvFileProcessor;

        public Stopwatch StartMeasure()
        {
            var timer = new Stopwatch();
            timer.Start();
            return timer;
        }

        public void StopMeasure(Stopwatch timer)
        {
            timer.Stop();
            _operationTime.Add(timer.ElapsedMilliseconds);
        }

        public void AddResult(bool result)
        {
            if (!result) Interlocked.Increment(ref _failCount);

            Interlocked.Increment(ref _totalCount);
        }

        public void AddOperationTime(long mls)
        {
            _operationTime.Add(mls);

        }

        public abstract void Tick();

        public void SetCsvFileProcessor(CsvFileProcessor csvFileProcessor)
        {
            _csvFileProcessor = csvFileProcessor;
            if (_csvFileProcessor != null)
                RegistrateColumns(csvFileProcessor);
        }

        public virtual string TotalStatistics()
        {
            return string.Format("Name: {0}\nTotalCount: {1}\nFailCount: {2}\nOperation AvgTime: {3}mls", Name,
                TotalCount,
                FailCount, _operationTime.Sum()/_operationTime.Count);
        }

        protected string GetCsvColumnName(string columnName)
        {
            return Name + columnName;
        }

        protected virtual void RegistrateColumns(CsvFileProcessor csvFileProcessor)
        {
        }

        protected void CsvFileWrite(string columnName, object value)
        {
            if (_csvFileProcessor != null)
                _csvFileProcessor.Write(columnName, value);
        }
    }
}

using System;
using Qoollo.Benchmark.Commands;
using Qoollo.Benchmark.DataGenerator;
using Qoollo.Concierge;
using Qoollo.Concierge.Attributes;

namespace Qoollo.Benchmark.Executor
{
    [DefaultExecutor]
    class BenchmarkExecutor:IUserExecutable
    {
        private readonly WriterExecutor _writerExecutor;
        private readonly CollectorExecutor _collectorExecutor;

        public BenchmarkExecutor()
        {
            AppDomain.CurrentDomain.FirstChanceException += (e, sender) => Console.WriteLine(e);
            _writerExecutor = new WriterExecutor();
            _collectorExecutor = new CollectorExecutor();
        }

        public void AddDataGenerator(string generatorName, IDataGenerator generator)
        {
            _writerExecutor.AddDataGenerator(generatorName, generator);
        }                

        [CommandHandler("writer", "Run DbWriter benchmark")]
        public string WriterCommandHandler(WriterCommand command)
        {
            return _writerExecutor.ProcessBenchmark(command);
        }

        [CommandHandler("collector", "Run Collector benchmark")]
        public string ReaderCommandHandler(CollectorCommand command)
        {
            return _collectorExecutor.ProcessBenchmark(command);
        }

        #region Executor

        public void Dispose()
        {            
        }

        public void Start()
        {            
        }

        public void Stop()
        {            
        }

        public IWindowsServiceConfig Configuration
        {
            get { return new WinServiceConfig
            {
                Async = true,
                Description = "Service for BriksDb benchmark",
                DisplayName = "BriksDb.Benchmark",
                InstallName = "BriksDb.Benchmark",
                StartAfterInstall = true
            }; }
        }

        #endregion
    }
}

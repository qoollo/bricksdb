using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Benchmark.DataGenerator;
using Qoollo.Benchmark.Load;
using Qoollo.Benchmark.Send;
using Qoollo.Concierge;
using Qoollo.Concierge.Attributes;
using Qoollo.Concierge.Commands;

namespace Qoollo.Benchmark
{
    // benchmark -n <count> -t <set> -h <host> -p <port> -c <threads>

    internal class BenchmarkBaseCommand : UserCommand
    {
        [Parameter(ShortKey = 't', IsRequired = false, Description = "Table name", DefaultValue = "BenchmarkTable")]
        public string TableName { get; set; }

        [Parameter(ShortKey = 'n', IsRequired = false, Description = "Count data", DefaultValue = -1)]
        public int DataCount { get; set; }

        [Parameter(ShortKey = 'l', IsRequired = true, Description = "Test type")]
        public string TestType { get; set; }

        [Parameter(ShortKey = 'h', IsRequired = true, Description = "Connection host")]
        public string Host { get; set; }

        [Parameter(ShortKey = 'p', IsRequired = true, Description = "Connection port")]
        public int Port { get; set; }

        [Parameter(ShortKey = 'c', IsRequired = false, Description = "Count threads", DefaultValue = 1)]
        public int ThreadsCount { get; set; }

        [Parameter(ShortKey = 'r', IsRequired = false, Description = "KeyRange", DefaultValue = 1000000)]
        public int KeyRange { get; set; }

        [Parameter(ShortKey = 'g', IsRequired = false, Description = "Generator type", DefaultValue = "default")]
        public string Generator { get; set; }
    }

    [DefaultExecutor]
    class BenchmarkExecutor:IUserExecutable
    {
        private readonly Dictionary<string, IDataGenerator> _dataGenerators;
        
  
        public BenchmarkExecutor()
        {
            _dataGenerators = new Dictionary<string, IDataGenerator>();            
            AddDataGenerator("default", new DefaultDataGenerator());
        }

        private IDataGenerator FindGenerator(string generatorName)
        {
            generatorName = generatorName.Trim().ToLower();

            IDataGenerator generator;
            if (_dataGenerators.TryGetValue(generatorName, out generator))
                return generator;

            throw new InitializationException(string.Format("Key {0} for generator not found", generatorName));
        }

        public void AddDataGenerator(string generatorName, IDataGenerator generator)
        {
            _dataGenerators.Add(generatorName, generator);
        }

        private Func<LoadTest> CreateSetTest(Func<DataSender> senderFactory, string generatorName, KeyGenerator keyGenerator)
        {
            return () => new SetLoadTest(senderFactory, FindGenerator(generatorName), keyGenerator);
        }

        private IEnumerable<Func<LoadTest>> ParseTestTypes(string testType, Func<DataSender> senderFactory, string generatorName,
            KeyGenerator keyGenerator)
        {
            var testNames = testType.ToLower().Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
            var ret = new List<Func<LoadTest>>();

            foreach (var name in testNames)
            {
                if (string.Equals(name, "set"))
                    ret.Add(CreateSetTest(senderFactory, generatorName, keyGenerator));
            }
            return ret;
        }

        [CommandHandler("writer", "Run DbWriter benchmark")]
        public string TestCommand(BenchmarkBaseCommand command)
        {
            try
            {
                var benchmark = new BenchmarkTest(command.ThreadsCount, command.DataCount);

                var testTypes = ParseTestTypes(command.TestType,
                    () => new DbWriterSender(command.Host, command.Port, command.TableName),
                    command.Generator, new KeyGenerator(command.KeyRange));
                foreach (var func in testTypes)
                {
                    benchmark.AddLoadTestFactory(func);
                }

                benchmark.Run();
            }
            catch (Exception e)
            {
                return e.Message;
            }

            return string.Empty;
        }


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
    }
}

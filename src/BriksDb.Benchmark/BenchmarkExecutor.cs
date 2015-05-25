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

namespace Qoollo.Benchmark
{
    [DefaultExecutor]
    class BenchmarkExecutor:IUserExecutable
    {
        private readonly Dictionary<string, IDataGenerator> _dataGenerators;
        
  
        public BenchmarkExecutor()
        {
            AppDomain.CurrentDomain.FirstChanceException += (e, sender) => Console.WriteLine(e);
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

        private Func<LoadTest> CreateGetTest(Func<DataSender> senderFactory,  KeyGenerator keyGenerator)
        {
            return () => new GetLoadTest(senderFactory, keyGenerator);
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
                if (string.Equals(name, "get"))
                    ret.Add(CreateGetTest(senderFactory, keyGenerator));
            }
            return ret;
        }

        [CommandHandler("writer", "Run DbWriter benchmark")]
        public string TestCommand(BenchmarkCommand command)
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

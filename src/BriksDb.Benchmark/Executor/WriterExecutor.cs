using System;
using System.Collections.Generic;
using Qoollo.Benchmark.Commands;
using Qoollo.Benchmark.DataGenerator;
using Qoollo.Benchmark.Load;
using Qoollo.Benchmark.Send;
using Qoollo.Concierge;

namespace Qoollo.Benchmark.Executor
{
    class WriterExecutor
    {
        private readonly Dictionary<string, IDataGenerator> _dataGenerators;
        public WriterExecutor()
        {
            _dataGenerators = new Dictionary<string, IDataGenerator>();            
            AddDataGenerator("default", new DefaultDataGenerator());
        }

        public void AddDataGenerator(string generatorName, IDataGenerator generator)
        {
            _dataGenerators.Add(generatorName, generator);
        }

        private IDataGenerator FindGenerator(string generatorName)
        {
            generatorName = generatorName.Trim().ToLower();

            IDataGenerator generator;
            if (_dataGenerators.TryGetValue(generatorName, out generator))
                return generator;

            throw new InitializationException(string.Format("Key {0} for generator not found", generatorName));
        }

        private IEnumerable<Func<LoadTest>> ParseTestTypes(string testType, Func<DbWriterAdapter> senderFactory, string generatorName,
           KeyGenerator keyGenerator)
        {
            var testNames = testType.ToLower().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
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

        private Func<LoadTest> CreateSetTest(Func<DbWriterAdapter> senderFactory, string generatorName, KeyGenerator keyGenerator)
        {
            return () => new SetLoadTest(senderFactory(), FindGenerator(generatorName), keyGenerator);
        }

        private Func<LoadTest> CreateGetTest(Func<DbWriterAdapter> senderFactory, KeyGenerator keyGenerator)
        {
            return () => new GetLoadTest(senderFactory(), keyGenerator);
        }

        public string ProcessBenchmark(WriterCommand command)
        {
            try
            {
                var benchmark = new BenchmarkTest(command.ThreadsCount, command.DataCount);

                var testTypes = ParseTestTypes(command.TestType,
                    () => new DbWriterAdapter(command.Host, command.Port, command.TableName),
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
    }
}

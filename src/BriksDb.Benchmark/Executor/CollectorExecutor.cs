using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Qoollo.Benchmark.Commands;
using Qoollo.Benchmark.Load;
using Qoollo.Benchmark.Send;
using Qoollo.Client.WriterGate;
using Qoollo.Concierge;

namespace Qoollo.Benchmark.Executor
{
    class CollectorExecutor
    {
        private readonly Dictionary<string, DbFactory> _dbFactories
            ;
        public CollectorExecutor()
        {
            _dbFactories = new Dictionary<string, DbFactory>();
        }

        public void AddDbFactory(string name, DbFactory dbFactory)
        {
            _dbFactories.Add(name, dbFactory);
        }

        private DbFactory FindDbFactory(string dbFactoryName)
        {
            dbFactoryName = dbFactoryName.Trim().ToLower();

            DbFactory dbFactory;
            if (_dbFactories.TryGetValue(dbFactoryName, out dbFactory))
                return dbFactory;

            throw new InitializationException(string.Format("Key {0} for db factory not found", dbFactoryName));
        }

        private Func<LoadTest> CreateReaderTest(IEnumerable<QueryDescription> queries,
            string tableName, string hashFileName, int countReplics, int pageSize)
        {
            var queue = new BlockingQueue<QueryDescription>(queries);
            return () => new ReaderLoadTest(new ReaderAdapter(FindDbFactory(tableName), tableName,
                hashFileName, countReplics, pageSize), queue);
        }

        private IEnumerable<QueryDescription> ReadJsonFile(string fileName)
        {
            List<QueryDescription> items;
            using (var r = new StreamReader(fileName))
            {
                var json = r.ReadToEnd();
                items = JsonConvert.DeserializeObject<List<QueryDescription>>(json);
            }
            return items;
        }

        public string ProcessBenchmark(CollectorCommand command)
        {
            try
            {
                var benchmark = new BenchmarkTest(command.ThreadsCount);
                benchmark.AddLoadTestFactory(CreateReaderTest(ReadJsonFile(command.FileName), command.TableName,
                    command.HashFileName, command.CountReplics, command.PageSize));

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

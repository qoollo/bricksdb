using System;
using Qoollo.Client.CollectorGate;
using Qoollo.Client.Configuration;
using Qoollo.Client.WriterGate;

namespace Qoollo.Benchmark.Send
{
    class ReaderAdapter:IDataAdapter
    {
        private readonly CollectorGate _collector;

        public ReaderAdapter(DbFactory dbFactory, string tableName, string hashFileName, int countReplics, int pageSize)
        {
            _collector = new CollectorGate(tableName, dbFactory, 
                new CollectorConfiguration(hashFileName, countReplics, pageSize),
                new CollectorNetConfiguration(), new CommonConfiguration(), 
                new TimeoutConfiguration());
            _collector.Build();            
        }

        public void Start()
        {
            _collector.Start();
        }

        public StorageDbReader ExecuteQuery(QueryDescription query)
        {
            return _collector.Api.CreateReader(query.QueryScript);
        }

        public void Dispose()
        {
            _collector.Dispose();
        }
    }
}

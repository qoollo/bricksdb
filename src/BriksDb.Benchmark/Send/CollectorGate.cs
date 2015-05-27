using System.Diagnostics.Contracts;
using Qoollo.Client.CollectorGate;
using Qoollo.Client.Configuration;
using Qoollo.Client.WriterGate;

namespace Qoollo.Benchmark.Send
{
    internal class CollectorGate : CollectorApi
    {
        public ICollectorApi Api { get { return _api; } }

        public CollectorGate(string tableName, DbFactory dbFactory,
            CollectorConfiguration collectorConfiguration, CollectorNetConfiguration netConfiguration,
            CommonConfiguration commonConfiguration, TimeoutConfiguration timeoutConfiguration)
            : base(collectorConfiguration, netConfiguration, commonConfiguration, timeoutConfiguration)
        {
            Contract.Requires(!string.IsNullOrEmpty(tableName));
            Contract.Requires(_dbFactory != null);
            _tableName = tableName;
            _dbFactory = dbFactory;
        }

        private readonly string _tableName;
        private readonly DbFactory _dbFactory;
        private ICollectorApi _api;

        protected override void InnerBuild()
        {
            _api = RegistrateApi(_tableName, _dbFactory);            
        }
    }
}
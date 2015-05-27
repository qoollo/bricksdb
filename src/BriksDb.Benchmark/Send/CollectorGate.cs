using System.Diagnostics.Contracts;
using Qoollo.Client.CollectorGate;
using Qoollo.Client.Configuration;

namespace Qoollo.Benchmark.Send
{
    internal class CollectorGate : CollectorApi
    {
        public ICollectorApi Api { get { return _api; } }

        public CollectorGate(string tableName,
            CollectorConfiguration collectorConfiguration, CollectorNetConfiguration netConfiguration,
            CommonConfiguration commonConfiguration, TimeoutConfiguration timeoutConfiguration)
            : base(collectorConfiguration, netConfiguration, commonConfiguration, timeoutConfiguration)
        {
            Contract.Requires(!string.IsNullOrEmpty(tableName));
            _tableName = tableName;
        }

        private readonly string _tableName;
        private ICollectorApi _api;

        protected override void InnerBuild()
        {
            _api = RegistrateApi(_tableName, null);            
        }
    }
}
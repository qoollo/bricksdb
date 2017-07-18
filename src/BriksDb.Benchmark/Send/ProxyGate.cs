using System.Diagnostics.Contracts;
using Qoollo.Client.Configuration;
using Qoollo.Client.ProxyGate;

namespace Qoollo.Benchmark.Send
{
    internal class ProxyGate : ProxyApi
    {
        public IStorage<long, string> Api { get { return _api; } }
        
        public ProxyGate(string tableName, NetConfiguration netConfiguration, ProxyConfiguration proxyConfiguration)
            : base(netConfiguration, proxyConfiguration)
        {
            Contract.Requires(!string.IsNullOrEmpty(tableName));
            _tableName = tableName;
        }

        private readonly string _tableName;
        private IStorage<long, string> _api;

        protected override void InnerBuild()
        {
            _api = RegistrateApi(_tableName, false, new DataProvider());
        }
    }
}

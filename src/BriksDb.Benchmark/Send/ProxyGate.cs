using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Client.Configuration;
using Qoollo.Client.ProxyGate;

namespace Qoollo.Benchmark.Send
{
    internal class ProxyGate : ProxyApi
    {
        public IStorage<long, string> Api { get { return _api; } }
        
        public ProxyGate(string tableName, NetConfiguration netConfiguration, ProxyConfiguration proxyConfiguration,
            CommonConfiguration commonConfiguration, TimeoutConfiguration timeoutConfiguration)
            : base(netConfiguration, proxyConfiguration, commonConfiguration, timeoutConfiguration)
        {
            Contract.Requires(!string.IsNullOrEmpty(tableName));
            _tableName = tableName;
        }

        public ProxyGate(string tableName, NetConfiguration netConfiguration, ProxyConfiguration proxyConfiguration,
            CommonConfiguration commonConfiguration) : base(netConfiguration, proxyConfiguration, commonConfiguration)
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

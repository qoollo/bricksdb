using System.Diagnostics.Contracts;
using System.ServiceModel;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.NetInterfaces.Data;

namespace Qoollo.Impl.Modules.Net.ReceiveBehavior
{
    internal class NetReceiveBehavior<TReceive> : ReceiveBehaviorBase<TReceive>
    {
        private readonly NetReceiverConfiguration _configuration;
        private readonly object _server;
        private ServiceHost _host;

        public NetReceiveBehavior(NetReceiverConfiguration configuration, object server) 
            : base(configuration, server)
        {
            Contract.Requires(configuration != null);
            _configuration = configuration;
            _server = server;
        }

        public override void Start()
        {
            if (_configuration.Host != "fake" && _configuration.Service != "fake"
                && _configuration.Port != 157)
                _host = NetConnector.CreateServer<TReceive>(_server, _configuration);
        }

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall && _host != null)
                NetConnector.StopService(_host);
        }
    }
}
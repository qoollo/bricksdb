using System.Diagnostics.Contracts;
using System.ServiceModel;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.NetInterfaces.Data;

namespace Qoollo.Impl.Modules.Net
{
    internal abstract class NetReceiveModule<T> : ControlModule
    {
        private readonly NetReceiverConfiguration _configuration;
        private ServiceHost _host;

        protected NetReceiveModule(NetReceiverConfiguration configuration)
        {
            Contract.Requires(configuration != null);
            _configuration = configuration;
        }

        public override void Start()
        {
            if (_configuration.Host != "fake" && _configuration.Service != "fake" && _configuration.Port != 157)
                _host = NetConnector.CreateServer<T>(this, _configuration);
        }

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall && _host != null)
                NetConnector.StopService(_host);

            base.Dispose(isUserCall);
        }
    }
}

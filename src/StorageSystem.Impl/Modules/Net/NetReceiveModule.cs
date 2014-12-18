using System.Diagnostics.Contracts;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.NetInterfaces.Data;

namespace Qoollo.Impl.Modules.Net
{
    internal abstract class NetReceiveModule<T>:ControlModule
    {
        private NetReceiverConfiguration _configuration;

        protected NetReceiveModule(NetReceiverConfiguration configuration)
        {
            Contract.Requires(configuration!=null);
            _configuration = configuration;
        }

        public override void Start()
        {
            if (_configuration.Host != "fake" && _configuration.Service != "fake" && _configuration.Port!=157)
                NetConnector.CreateServer<T>(this, _configuration);
        }      
    }
}

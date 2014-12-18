using System.Diagnostics.Contracts;
using System.ServiceModel;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Net;
using Qoollo.Impl.NetInterfaces.Proxy;

namespace Qoollo.Impl.Proxy.ProxyNet
{
    internal class ProxyNetReceiver : NetReceiveModule<ICommonProxyNetReceiver>, ICommonProxyNetReceiver
    {
        private ProxyDistributorModule _distributorModule;

        public ProxyNetReceiver(ProxyDistributorModule distributorModule, NetReceiverConfiguration receiverConfiguration)
            :base(receiverConfiguration)
        {
            Contract.Requires(distributorModule!=null);
            _distributorModule = distributorModule;
        }

        [OperationBehavior(TransactionScopeRequired = true)]
        public RemoteResult SendSync(NetCommand command)
        {
            return _distributorModule.ProcessNetCommand(command);
        }        

        public RemoteResult Ping()
        {
            return new SuccessResult();
        }

        public void SendASync(NetCommand command)
        {
            _distributorModule.ProcessNetCommand(command);
        }

    }
}
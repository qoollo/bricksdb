using System.ServiceModel;
using Ninject;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Net;
using Qoollo.Impl.NetInterfaces.Proxy;
using Qoollo.Impl.Proxy.Interfaces;

namespace Qoollo.Impl.Proxy.ProxyNet
{
    internal class ProxyNetReceiver : NetReceiveModule<ICommonProxyNetReceiver>, ICommonProxyNetReceiver
    {
        private IProxyDistributorModule _distributorModule;

        public ProxyNetReceiver(StandardKernel kernel, NetReceiverConfiguration receiverConfiguration)
            :base(kernel, receiverConfiguration)
        {
        }

        public override void Start()
        {
            _distributorModule = Kernel.Get<IProxyDistributorModule>();

            base.Start();
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
using System.ServiceModel;
using Ninject;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DistributorModules.Interfaces;
using Qoollo.Impl.Modules.Net;
using Qoollo.Impl.NetInterfaces.Distributor;

namespace Qoollo.Impl.DistributorModules.DistributorNet
{
    internal class NetDistributorReceiverForDb : NetReceiveModule<ICommonNetReceiverForDb>, ICommonNetReceiverForDb
    {        
        private IDistributorModule _distributorModule;

        public NetDistributorReceiverForDb(StandardKernel kernel, NetReceiverConfiguration receiverConfiguration)
            : base(kernel, receiverConfiguration)
        {
        }

        public override void Start()
        {
            _distributorModule = Kernel.Get<IDistributorModule>();

            base.Start();
        }

        public RemoteResult SendSync(NetCommand command)
        {
            return _distributorModule.Execute<NetCommand, RemoteResult>(command);
        }

        public void SendASync(NetCommand command)
        {
            _distributorModule.Execute<NetCommand, RemoteResult>(command);
        }

        public RemoteResult Ping()
        {
            return new SuccessResult();
        }

        [OperationBehavior(TransactionScopeRequired = true)]
        public void TransactionAnswer(Common.Data.TransactionTypes.Transaction transaction)
        {
            _distributorModule.ProcessTransaction(transaction);
        }
    }
}

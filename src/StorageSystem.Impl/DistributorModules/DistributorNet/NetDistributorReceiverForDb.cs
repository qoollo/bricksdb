using System.Diagnostics.Contracts;
using System.ServiceModel;
using Ninject;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Net;
using Qoollo.Impl.NetInterfaces.Distributor;

namespace Qoollo.Impl.DistributorModules.DistributorNet
{
    internal class NetDistributorReceiverForDb : NetReceiveModule<ICommonNetReceiverForDb>, ICommonNetReceiverForDb
    {        
        private readonly DistributorModule _distributorModule;

        public NetDistributorReceiverForDb(StandardKernel kernel, DistributorModule distributorModule,
            NetReceiverConfiguration receiverConfiguration)
            : base(kernel, receiverConfiguration)
        {
            Contract.Requires(distributorModule != null);
            _distributorModule = distributorModule;
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

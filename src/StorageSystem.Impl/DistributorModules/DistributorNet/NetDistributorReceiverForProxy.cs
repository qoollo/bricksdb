using System.Diagnostics.Contracts;
using System.ServiceModel;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DistributorModules.ParallelWork;
using Qoollo.Impl.Modules.Net;
using Qoollo.Impl.NetInterfaces.Distributor;

namespace Qoollo.Impl.DistributorModules.DistributorNet
{
    internal class NetDistributorReceiverForProxy : NetReceiveModule<ICommonNetReceiverForProxy>, ICommonNetReceiverForProxy
    {
        private MainLogicModule _mainLogic;
        private IInputModule _input;
        private DistributorModule _distributorModule;

        public NetDistributorReceiverForProxy(MainLogicModule main, IInputModule input, DistributorModule distributorModule,
                                      NetReceiverConfiguration receiverConfiguration):base(receiverConfiguration)
        {
            Contract.Requires(main != null);
            Contract.Requires(input != null);
            Contract.Requires(distributorModule!=null);
            _mainLogic = main;
            _input = input;
            _distributorModule = distributorModule;
        }

        [OperationBehavior(TransactionScopeRequired = true)]
        public void Process(InnerData ev)
        {            
            _input.ProcessAsync(ev);
        }

        [OperationBehavior(TransactionScopeRequired = true)]
        public UserTransaction GetTransaction(UserTransaction transaction)
        {
            return _mainLogic.GetTransactionState(transaction);
        }

        public RemoteResult SendSync(NetCommand command)
        {
            return _distributorModule.ProcessNetCommand(command);
        }

        public void SendASync(NetCommand command)
        {
            _distributorModule.ProcessNetCommand(command);
        }

        public RemoteResult Ping()
        {
            return new SuccessResult();
        }
    }
}

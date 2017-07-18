﻿using System.ServiceModel;
using Ninject;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Configurations.Queue;
using Qoollo.Impl.DistributorModules.Interfaces;
using Qoollo.Impl.Modules.Net;
using Qoollo.Impl.NetInterfaces.Distributor;

namespace Qoollo.Impl.DistributorModules.DistributorNet
{
    internal class NetDistributorReceiverForProxy : NetReceiveModule<ICommonNetReceiverForProxy>, ICommonNetReceiverForProxy
    {
        private IMainLogicModule _mainLogic;
        private IInputModule _input;
        private IDistributorModule _distributorModule;

        public NetDistributorReceiverForProxy(StandardKernel kernel, NetConfiguration receiverConfiguration)
            : base(kernel, receiverConfiguration)
        {
        }

        public override void Start()
        {
            _distributorModule = Kernel.Get<IDistributorModule>();
            _input = Kernel.Get<IInputModule>();
            _mainLogic = Kernel.Get<IMainLogicModule>();

            base.Start();
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
            return _distributorModule.Execute<NetCommand, RemoteResult>(command);
            //return _distributorModule.ProcessNetCommand(command);
        }

        public void SendASync(NetCommand command)
        {
            _distributorModule.Execute<NetCommand, RemoteResult>(command);
            //_distributorModule.ProcessNetCommand(command);
        }

        public RemoteResult Ping()
        {
            return new SuccessResult();
        }
    }
}

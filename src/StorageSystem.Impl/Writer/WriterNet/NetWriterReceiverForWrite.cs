using System;
using System.Diagnostics.Contracts;
using System.ServiceModel;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Net;
using Qoollo.Impl.NetInterfaces.Writer;
using Qoollo.Impl.Writer.Distributor;

namespace Qoollo.Impl.Writer.WriterNet
{
    internal class NetWriterReceiverForWrite: NetReceiveModule<ICommonNetReceiverWriterForWrite>, ICommonNetReceiverWriterForWrite
    {
        private InputModule _inputModule;
        private DistributorModule _distributor;

        public NetWriterReceiverForWrite(InputModule inputModule, DistributorModule distributor, NetReceiverConfiguration receiverConfiguration)
            :base(receiverConfiguration)
        {
            Contract.Requires(inputModule!=null);
            Contract.Requires(distributor!=null);
            _distributor = distributor;
            _inputModule = inputModule;
        }

        [OperationBehavior(TransactionScopeRequired = true)]
        public void Process(InnerData data)
        {
            _inputModule.Process(data);
        }
        
        public RemoteResult ProcessSync(InnerData data)
        {
            return _inputModule.ProcessSync(data);
        }

        [OperationBehavior(TransactionScopeRequired = true)]
        public void Rollback(InnerData data)
        {
            _inputModule.Rollback(data);
        }

        public InnerData ReadOperation(InnerData data)
        {
            return _inputModule.ReadOperation(data);
        }

        [OperationBehavior(TransactionScopeRequired = true)]
        public RemoteResult SendSync(NetCommand command)
        {
            return _distributor.ProcessSend(command);
        }

        public void SendASync(NetCommand command)
        {
            throw new NotImplementedException();
        }

        public RemoteResult Ping()
        {
            return new SuccessResult();
        }
    }
}

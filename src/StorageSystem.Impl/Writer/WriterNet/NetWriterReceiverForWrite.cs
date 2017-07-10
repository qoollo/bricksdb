using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using Ninject;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Net;
using Qoollo.Impl.NetInterfaces.Writer;
using Qoollo.Impl.Writer.Interfaces;

namespace Qoollo.Impl.Writer.WriterNet
{
    internal class NetWriterReceiverForWrite: NetReceiveModule<ICommonNetReceiverWriterForWrite>, ICommonNetReceiverWriterForWrite
    {
        private IInputModule _inputModule;
        private IDistributorModule _distributor;

        public NetWriterReceiverForWrite(StandardKernel kernel, NetReceiverConfiguration receiverConfiguration)
            :base(kernel, receiverConfiguration)
        {
        }

        public override void Start()
        {
            _inputModule = Kernel.Get<IInputModule>();
            _distributor = Kernel.Get<IDistributorModule>();

            base.Start();
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

        public RemoteResult ProcessSyncPackage(List<InnerData> datas)
        {
            return _inputModule.ProcessSyncPackage(datas);
        }

        [OperationBehavior(TransactionScopeRequired = true)]
        public void Rollback(InnerData data)
        {
            _inputModule.Rollback(data);
        }

        public Task<RemoteResult> ProcessTaskBased(InnerData data)
        {
            return Task.FromResult(ProcessSync(data));
        }


        public InnerData ReadOperation(InnerData data)
        {
            return _inputModule.ReadOperation(data);
        }

        [OperationBehavior(TransactionScopeRequired = true)]
        public RemoteResult SendSync(NetCommand command)
        {
            return _distributor.Execute<NetCommand, RemoteResult>(command);
        }

        public void SendASync(NetCommand command)
        {
            _distributor.Execute<NetCommand, RemoteResult>(command);
        }

        public RemoteResult Ping()
        {
            return new SuccessResult();
        }
    }
}

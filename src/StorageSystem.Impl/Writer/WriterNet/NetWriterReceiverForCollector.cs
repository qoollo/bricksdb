using System;
using System.ServiceModel;
using Ninject;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Net;
using Qoollo.Impl.NetInterfaces.Data;
using Qoollo.Impl.NetInterfaces.Writer;
using Qoollo.Impl.Writer.Interfaces;

namespace Qoollo.Impl.Writer.WriterNet
{
    internal class NetWriterReceiverForCollector:NetReceiveModule<ICommonNetReceiverWriterForCollector>, ICommonNetReceiverWriterForCollector
    {
        private IInputModule _inputModule;
        private IDistributorModule _distributor;

        public NetWriterReceiverForCollector(StandardKernel kernel, NetConfiguration configuration)
            : base(kernel, configuration)
        {
        }

        public override void Start()
        {
            _inputModule = Kernel.Get<IInputModule>();
            _distributor = Kernel.Get<IDistributorModule>();

            base.Start();
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

        public Tuple<RemoteResult, SelectSearchResult> SelectQuery(SelectDescription description)
        {
            return _inputModule.SelectQuery(description);
        }
    }
}

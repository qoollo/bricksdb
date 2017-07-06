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

namespace Qoollo.Impl.Writer.WriterNet
{
    internal class NetWriterReceiverForCollector:NetReceiveModule<ICommonNetReceiverWriterForCollector>, ICommonNetReceiverWriterForCollector
    {
        private readonly InputModule _inputModule;
        private readonly DistributorModule _distributor;

        public NetWriterReceiverForCollector(StandardKernel kernel, InputModule inputModule, DistributorModule distributor, NetReceiverConfiguration configuration)
            : base(kernel, configuration)
        {
            _inputModule = inputModule;
            _distributor = distributor;
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

using System;
using System.ServiceModel;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DbController.Distributor;
using Qoollo.Impl.Modules.Net;
using Qoollo.Impl.NetInterfaces.Data;
using Qoollo.Impl.NetInterfaces.DbController;

namespace Qoollo.Impl.DbController.DbControllerNet
{
    internal class NetDbControllerReceiverForCollector:NetReceiveModule<ICommonNetReceiverDbControllerForCollector>, ICommonNetReceiverDbControllerForCollector
    {
        private readonly InputModule _inputModule;
        private readonly DistributorModule _distributor;

        public NetDbControllerReceiverForCollector(InputModule inputModule, DistributorModule distributor, NetReceiverConfiguration configuration)
            : base(configuration)
        {
            _inputModule = inputModule;
            _distributor = distributor;
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

        public Tuple<RemoteResult, SelectSearchResult> SelectQuery(SelectDescription description)
        {
            return _inputModule.SelectQuery(description);
        }
    }
}

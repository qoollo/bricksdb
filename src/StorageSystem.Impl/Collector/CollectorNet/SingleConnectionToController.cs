using System;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.NetResults.Event;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Net;
using Qoollo.Impl.NetInterfaces;
using Qoollo.Impl.NetInterfaces.Data;
using Qoollo.Impl.NetInterfaces.DbController;

namespace Qoollo.Impl.Collector.CollectorNet
{
    internal class SingleConnectionToController : SingleConnection<ICommonNetReceiverDbControllerForCollector>,
        ICommonNetReceiverDbControllerForCollector, ISingleConnection
    {
        public SingleConnectionToController(ServerId server, ConnectionConfiguration configuration,
            ConnectionTimeoutConfiguration timeoutConfiguration) : base(server, configuration, timeoutConfiguration)
        {
        }

        public Tuple<RemoteResult, SelectSearchResult> SelectQuery(SelectDescription description)
        {
            return SendFunc<Tuple<RemoteResult, SelectSearchResult>, ICommonNetReceiverDbControllerForCollector>(
                api => api.SelectQuery(description),
                e => new Tuple<RemoteResult, SelectSearchResult>(new ServerNotAvailable(Server), null),
                NetLogHelper.GetLog(description));
        }

        public RemoteResult Ping()
        {
            return SendFunc<RemoteResult, ICommonNetReceiverDbControllerForCollector>(
                api => api.Ping(),
                e => new ServerNotAvailable(Server), "");
        }

        #region not implemented
        
        public RemoteResult SendSync(NetCommand command)
        {
            throw new NotImplementedException();
        }

        public void SendASync(NetCommand command)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}

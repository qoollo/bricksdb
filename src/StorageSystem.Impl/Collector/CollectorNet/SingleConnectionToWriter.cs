using System;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.NetResults.Event;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Net;
using Qoollo.Impl.NetInterfaces;
using Qoollo.Impl.NetInterfaces.Data;
using Qoollo.Impl.NetInterfaces.Writer;

namespace Qoollo.Impl.Collector.CollectorNet
{
    internal class SingleConnectionToWriter : SingleConnection<ICommonNetReceiverWriterForCollector>,
        ICommonNetReceiverWriterForCollector, ISingleConnection
    {
        public SingleConnectionToWriter(ServerId server, ConnectionConfiguration configuration,
            ConnectionTimeoutConfiguration timeoutConfiguration) : base(server, configuration, timeoutConfiguration)
        {
        }

        public Tuple<RemoteResult, SelectSearchResult> SelectQuery(SelectDescription description)
        {
            return SendFunc<Tuple<RemoteResult, SelectSearchResult>, ICommonNetReceiverWriterForCollector>(
                api => api.SelectQuery(description),
                e => new Tuple<RemoteResult, SelectSearchResult>(new ServerNotAvailable(Server), null),
                NetLogHelper.GetLog(description));
        }

        public RemoteResult Ping()
        {
            return SendFunc<RemoteResult, ICommonNetReceiverWriterForCollector>(
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

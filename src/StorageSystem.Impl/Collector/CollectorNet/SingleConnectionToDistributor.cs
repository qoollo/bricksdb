using System;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.NetResults.Event;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Net;
using Qoollo.Impl.NetInterfaces;
using Qoollo.Impl.NetInterfaces.Distributor;

namespace Qoollo.Impl.Collector.CollectorNet
{
    internal class SingleConnectionToDistributor : SingleConnection<ICommonNetReceiverForDb>, ICommonNetReceiverForDb,
        ISingleConnection
    {
        public SingleConnectionToDistributor(ServerId server, ConnectionConfiguration configuration,
            ConnectionTimeoutConfiguration timeoutConfiguration) : base(server, configuration, timeoutConfiguration)
        {
        }        

        public RemoteResult Ping()
        {
            return SendFunc<RemoteResult, ICommonNetReceiverForDb>(
                 api => api.Ping(),
                 e => new ServerNotAvailable(Server),
                 "");
        }

        public RemoteResult SendSync(NetCommand command)
        {
            return SendFunc<RemoteResult, ICommonNetReceiverForDb>(
                 api => api.SendSync(command),
                 e => new ServerNotAvailable(Server),
                 NetLogHelper.GetLog(command));
        }

        #region NotImplemented

        public void SendASync(NetCommand command)
        {
            throw new NotImplementedException();
        }

        public void TransactionAnswer(Transaction transaction)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}

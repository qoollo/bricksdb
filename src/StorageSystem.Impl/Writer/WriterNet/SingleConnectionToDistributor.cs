using System;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.Event;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Net;
using Qoollo.Impl.NetInterfaces;
using Qoollo.Impl.NetInterfaces.Distributor;

namespace Qoollo.Impl.Writer.WriterNet
{
    internal class SingleConnectionToDistributor : SingleConnection<ICommonNetReceiverForDb>, ICommonNetReceiverForDb, ISingleConnection
    {
        public SingleConnectionToDistributor(ServerId server, ConnectionConfiguration configuration,
            ConnectionTimeoutConfiguration timeoutConfiguration) : base(server, configuration, timeoutConfiguration)
        {
        }

        public RemoteResult SendSync(NetCommand command)
        {
            return SendFunc<RemoteResult, ICommonNetReceiverForDb>(
                api => api.SendSync(command),
                e => new ServerNotAvailable(Server),
                NetLogHelper.GetLog(command));
        }

        public RemoteResult SendASyncResult(NetCommand command)
        {
            return SendFunc<RemoteResult, ICommonNetReceiverForDb>(
                api =>
                {
                    api.SendASync(command);
                    return new SuccessResult();
                },
                e => new ServerNotAvailable(Server),
                NetLogHelper.GetLog(command));
        }        

        public RemoteResult Ping()
        {
            return SendFunc<RemoteResult, ICommonNetReceiverForDb>(
                api => api.Ping(),
                e => new ServerNotAvailable(Server),
                "");
        }        

        public RemoteResult TransactionAnswerResult(Transaction transaction)
        {
            return SendFunc<RemoteResult, ICommonNetReceiverForDb>(
                api =>
                {
                    api.TransactionAnswer(transaction);
                    return new SuccessResult();
                },
                e => new ServerNotAvailable(Server),
                NetLogHelper.GetLog(transaction));
        }

        #region Not Implemented

        public void TransactionAnswer(Transaction transaction)
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

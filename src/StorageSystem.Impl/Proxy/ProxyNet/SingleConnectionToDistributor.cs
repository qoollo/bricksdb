using System;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.Event;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Net;
using Qoollo.Impl.NetInterfaces;
using Qoollo.Impl.NetInterfaces.Distributor;

namespace Qoollo.Impl.Proxy.ProxyNet
{
    internal class SingleConnectionToDistributor : SingleConnection<ICommonNetReceiverForProxy>,
        ICommonNetReceiverForProxy, ISingleConnection
    {
        public SingleConnectionToDistributor(ServerId server, ConnectionConfiguration configuration,
            ConnectionTimeoutConfiguration timeoutConfiguration) : base(server, configuration, timeoutConfiguration)
        {
        }

        public RemoteResult ProcessData(InnerData data)
        {
            return SendFunc<RemoteResult, ICommonNetReceiverForProxy>(
                api =>
                {
                    api.Process(data);
                    data.Transaction.PerfTimer.Complete();
                    PerfCounters.ProxyCounters.Instance.AllProcessPerSec.OperationFinished();

                    if(data.Transaction.OperationType == OperationType.Sync)
                        PerfCounters.ProxyCounters.Instance.SyncProcessPerSec.OperationFinished();
                    else
                        PerfCounters.ProxyCounters.Instance.AsyncProcessPerSec.OperationFinished();

                    return new SuccessResult();
                },
                e => new ServerNotAvailable(Server),
                NetLogHelper.GetLog(data));
        }

        public UserTransaction GetTransaction(UserTransaction transaction)
        {
            return SendFunc<UserTransaction, ICommonNetReceiverForProxy>(
                api => api.GetTransaction(transaction),
                e => null,
                NetLogHelper.GetLog(transaction));
        }

        public RemoteResult SendSync(NetCommand command)
        {
            return SendFunc<RemoteResult, ICommonNetReceiverForProxy>(
                api => api.SendSync(command),
                e => new ServerNotAvailable(Server),
                NetLogHelper.GetLog(command));
        }

        public RemoteResult Ping()
        {
            return SendFunc<RemoteResult, ICommonNetReceiverForProxy>(
                api => api.Ping(),
                e => new ServerNotAvailable(Server),
                "");
        }

        #region Not Implemented

        public void Process(InnerData ev)
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

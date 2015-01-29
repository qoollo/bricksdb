using System;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.Event;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Net;
using Qoollo.Impl.NetInterfaces;
using Qoollo.Impl.NetInterfaces.Writer;

namespace Qoollo.Impl.DistributorModules.DistributorNet
{
    internal class SingleConnectionToWriter : SingleConnection<ICommonNetReceiverWriterForWrite>, ICommonNetReceiverWriterForWrite, ISingleConnection
    {
        public SingleConnectionToWriter(ServerId server, ConnectionConfiguration configuration,
            ConnectionTimeoutConfiguration timeoutConfiguration) : base(server, configuration, timeoutConfiguration)
        {
        }

        public RemoteResult ProcessSync(InnerData data)
        {
            return SendFunc<RemoteResult, ICommonNetReceiverWriterForWrite>(
                api =>api.ProcessSync(data) ,
                e => new ServerNotAvailable(Server),
                NetLogHelper.GetLog(data));
        }        

        public RemoteResult ProcessData(InnerData data)
        {
            return SendFunc<RemoteResult, ICommonNetReceiverWriterForWrite>(
                api =>
                {
                    api.Process(data);
                    return new SuccessResult();
                },
                e => new ServerNotAvailable(Server),
                NetLogHelper.GetLog(data));
        }

        public InnerData ReadOperation(InnerData data, out RemoteResult result)
        {
            RemoteResult res = null;
            var ret = SendFunc<InnerData, ICommonNetReceiverWriterForWrite>(
                api =>
                {
                    var r = api.ReadOperation(data);
                    res = new SuccessResult();
                    return r;
                },
                e =>
                {
                    res = new ServerNotAvailable(Server);
                    return null;
                },
                NetLogHelper.GetLog(data));

            result = res;
            return ret;
        }

        public RemoteResult RollbackData(InnerData data)
        {
            return SendFunc<RemoteResult, ICommonNetReceiverWriterForWrite>(
                api =>
                {
                    api.Rollback(data);
                    return new SuccessResult();
                },
                e => new ServerNotAvailable(Server),
                NetLogHelper.GetLog(data));
        }

        public RemoteResult SendSync(NetCommand command)
        {
            return SendFunc<RemoteResult, ICommonNetReceiverWriterForWrite>(
                api => api.SendSync(command),
                e => new ServerNotAvailable(Server),
                NetLogHelper.GetLog(command));
        }

        public RemoteResult Ping()
        {
            return SendFunc<RemoteResult, ICommonNetReceiverWriterForWrite>(
                 api => api.Ping(),
                 e => new ServerNotAvailable(Server),
                 "");
        }

        #region Not Implemented

        public void SendASync(NetCommand command)
        {
            throw new NotImplementedException();
        }

        public InnerData ReadOperation(InnerData data)
        {
            throw new NotImplementedException();
        }

        public void Rollback(InnerData data)
        {
            throw new NotImplementedException();
        }

        public void Process(InnerData data)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

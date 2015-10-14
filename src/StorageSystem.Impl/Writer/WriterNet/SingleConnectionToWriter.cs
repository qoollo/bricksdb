﻿using System;
using System.Threading.Tasks;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.NetResults.Event;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Net;
using Qoollo.Impl.NetInterfaces;
using Qoollo.Impl.NetInterfaces.Writer;

namespace Qoollo.Impl.Writer.WriterNet
{
    internal class SingleConnectionToWriter : SingleConnection<ICommonNetReceiverWriterForWrite>, ICommonNetReceiverWriterForWrite, ISingleConnection
    {
        public SingleConnectionToWriter(ServerId server, ConnectionConfiguration configuration,
            ConnectionTimeoutConfiguration timeoutConfiguration) : base(server, configuration, timeoutConfiguration)
        {
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

        public RemoteResult ProcessSync(InnerData data)
        {
            return SendFunc<RemoteResult, ICommonNetReceiverWriterForWrite>(
                api => api.ProcessSync(data),
                e => new ServerNotAvailable(Server),
                NetLogHelper.GetLog(data));
        }

        public Task<RemoteResult> ProcessTaskBased(InnerData data)
        {
            return SendAsyncFunc<RemoteResult, ICommonNetReceiverWriterForWrite>(
                api => api.ProcessTaskBased(data), e => new ServerNotAvailable(Server),
                NetLogHelper.GetLog(data));
        }


        #region Not Implemented

        public void Rollback(InnerData data)
        {
            throw new NotImplementedException();
        }

        public InnerData ReadOperation(InnerData data)
        {
            throw new NotImplementedException();
        }       

        public void Process(InnerData data)
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

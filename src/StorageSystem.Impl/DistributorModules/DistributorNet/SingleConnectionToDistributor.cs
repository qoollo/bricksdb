﻿using Ninject;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.NetResults.Event;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Net;
using Qoollo.Impl.NetInterfaces;

namespace Qoollo.Impl.DistributorModules.DistributorNet
{
    internal class SingleConnectionToDistributor : SingleConnection<ICommonCommunicationNet>, ICommonCommunicationNet,
        ISingleConnection
    {
        public SingleConnectionToDistributor(StandardKernel kernel, ServerId server,
            ConnectionTimeoutConfiguration timeoutConfiguration) 
            : base(kernel, server, timeoutConfiguration)
        {
        }

        public RemoteResult SendSync(NetCommand command)
        {
            return SendFunc<RemoteResult, ICommonCommunicationNet>(
                api => api.SendSync(command),
                e => new ServerNotAvailable(Server),
                NetLogHelper.GetLog(command));
        }

        public RemoteResult Ping()
        {
            return SendFunc<RemoteResult, ICommonCommunicationNet>(
                api => api.Ping(),
                e => new ServerNotAvailable(Server),
                "");
        }

        #region Not Implemented

        public void SendASync(NetCommand command)
        {
            throw new System.NotImplementedException();
        }

        #endregion
    }
}

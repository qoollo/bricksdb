using System;
using System.Collections.Generic;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Proxy.Interfaces
{
    internal interface IProxyNetModule
    {
        bool ConnectToDistributor(ServerId server);
        RemoteResult GetTransaction(ServerId server, UserTransaction transaction, out UserTransaction result);
        void PingDistributors(List<ServerId> servers, Action<ServerId> serverAvailable);
        RemoteResult Process(ServerId server, InnerData ev);
        RemoteResult SendDistributor(ServerId server, NetCommand command);
    }
}
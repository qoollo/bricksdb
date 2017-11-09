using System;
using System.Collections.Generic;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.DistributorModules.Interfaces
{
    internal interface IDistributorNetModule
    {
        RemoteResult ASendToProxy(ServerId server, NetCommand command);
        bool ConnectToDistributor(ServerId server);
        bool ConnectToProxy(ServerId server);
        bool ConnectToWriter(ServerId server);
        void PingDistributors(List<ServerId> servers);
        void PingProxy(List<ServerId> servers);
        void PingWriters(List<ServerId> servers, Action<ServerId> serverAvailable);
        RemoteResult Process(ServerId server, InnerData data);
        InnerData ReadOperation(ServerId server, InnerData data);
        RemoteResult Rollback(ServerId server, InnerData data);
        RemoteResult SendToDistributor(ServerId server, NetCommand command);
        RemoteResult SendToProxy(ServerId server, NetCommand command);
        RemoteResult SendToWriter(ServerId server, NetCommand command);

        List<ServerId> GetServersByType(Type type);
    }
}
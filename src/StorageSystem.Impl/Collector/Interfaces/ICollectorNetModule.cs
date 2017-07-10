using System;
using System.Collections.Generic;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.NetInterfaces.Data;

namespace Qoollo.Impl.Collector.Interfaces
{
    internal interface ICollectorNetModule
    {
        bool ConnectToDistributor(ServerId server);
        bool ConnectToWriter(ServerId server);
        void PingDistributors(List<ServerId> servers);
        void PingWriter(List<ServerId> servers, Action<ServerId> serverAvailable);
        Tuple<RemoteResult, SelectSearchResult> SelectQuery(ServerId server, SelectDescription description);
        RemoteResult SendSyncToDistributor(ServerId server, NetCommand command);

        List<ServerId> GetServersByType(Type type);
    }
}
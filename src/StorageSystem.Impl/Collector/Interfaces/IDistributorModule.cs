using System.Collections.Generic;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Collector.Interfaces
{
    internal interface IDistributorModule
    {
        List<ServerId> GetAvailableServers();
        SystemSearchStateInner GetState();
        string SayIAmHere(ServerId server);
        void ServerUnavailable(ServerId server);
    }
}
using System;
using System.Collections.Generic;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Collector.Interfaces
{
    internal interface ICollectorModel
    {
        bool UseStart { get; }

        bool CheckAliveServersWithStep(ServerId startServer);
        List<ServerId> GetAliveServersWithStep(ServerId startServer);
        List<ServerId> GetAllServers2();
        List<ServerId> GetAvailableServers();
        SystemSearchStateInner GetSystemState();
        List<ServerId> GetUnavailableServers();
        void NewServers(List<Tuple<ServerId, string, string>> servers);
        void ServerAvailable(ServerId serverId);
        void ServerNotAvailable(ServerId serverId);

        void Start();
    }
}
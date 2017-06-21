using Qoollo.Impl.Common.Server;

namespace Qoollo.Tests.NetMock
{
    internal interface INetMock
    {
        void AddServer<TReceive>(ServerId serverId, MockReceive<TReceive> receiveApi);
        void RemoveServer<TReceive>(ServerId serverId);
        bool TryConnectClient<TConnection>(ServerId serverId, out TConnection host);
    }
}
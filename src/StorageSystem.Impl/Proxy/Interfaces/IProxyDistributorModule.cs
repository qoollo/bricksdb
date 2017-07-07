using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Proxy.Interfaces
{
    internal interface IProxyDistributorModule
    {
        ServerId ProxyServerId { get; }

        Transaction CreateTransaction(string hash);
        ServerId GetDestination();
        ServerId GetDestination(UserTransaction transaction);
        RemoteResult ProcessNetCommand(NetCommand command);
        RemoteResult SayIAmHere(ServerId destination);
        void ServerNotAvailable(ServerId server);
    }
}
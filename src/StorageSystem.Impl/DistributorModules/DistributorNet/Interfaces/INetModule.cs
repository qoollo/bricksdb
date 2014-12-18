using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.DistributorModules.DistributorNet.Interfaces
{
    internal interface INetModule
    {
        RemoteResult Process(ServerId server, InnerData data);
        RemoteResult Rollback(ServerId server, InnerData data);
        RemoteResult SendToProxy(ServerId server, NetCommand command);
        RemoteResult ASendToProxy(ServerId server, NetCommand command);
        RemoteResult SendToDistributor(ServerId server, NetCommand command);

        InnerData ReadOperation(ServerId server, InnerData data);
    }
}

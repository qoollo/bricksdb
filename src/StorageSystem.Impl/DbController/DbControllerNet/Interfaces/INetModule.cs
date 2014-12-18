using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.DbController.DbControllerNet.Interfaces
{
    internal interface INetModule
    {
        RemoteResult SendToDistributor(ServerId server, NetCommand command);

        void ASendToDistributor(ServerId server, NetCommand command);
    }
}

using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Writer.WriterNet.Interfaces
{
    internal interface INetModule
    {
        RemoteResult SendToDistributor(ServerId server, NetCommand command);

        void ASendToDistributor(ServerId server, NetCommand command);
    }
}

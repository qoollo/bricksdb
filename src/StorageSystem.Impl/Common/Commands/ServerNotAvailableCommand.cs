using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Common.Commands
{
    internal class ServerNotAvailableCommand:NetCommand
    {
        public ServerId Server { get; private set; }

        public ServerNotAvailableCommand(ServerId server)
        {
            Server = server;
        }
    }
}

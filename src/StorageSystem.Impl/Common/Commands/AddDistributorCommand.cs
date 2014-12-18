using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Common.Commands
{
    internal class AddDistributorCommand:NetCommand
    {
        public string Hash { get; private set; }
        public ServerId Server { get; private set; }

        public AddDistributorCommand(string hash, ServerId server)
        {
            Server = server;
            Hash = hash;
        }
    }
}

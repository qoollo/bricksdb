using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Common.NetResults.Event
{
    internal class ServerNotAvailable : FailNetResult
    {        
        public ServerNotAvailable(ServerId server)
            : base(string.Format(Errors.ServerIsNotAvailableFormat, server.RemoteHost, server.Port))
        {
        }
    }
}

using System.Runtime.Serialization;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Common.NetResults.System.DbController
{
    [DataContract]
    internal class RestoreInProcessCommand:NetCommand
    {
        [DataMember]
        public ServerId ServerId { get; private set; }

        public RestoreInProcessCommand(ServerId serverId)
        {
            ServerId = serverId;
        }
    }
}

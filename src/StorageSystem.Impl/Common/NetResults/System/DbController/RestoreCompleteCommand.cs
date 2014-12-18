using System.Runtime.Serialization;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Common.NetResults.System.DbController
{
    [DataContract]
    internal class RestoreCompleteCommand:NetCommand
    {
        [DataMember]
        public ServerId ServerId { get; private set; }

        public RestoreCompleteCommand(ServerId serverId)
        {
            ServerId = serverId;
        }
    }
}

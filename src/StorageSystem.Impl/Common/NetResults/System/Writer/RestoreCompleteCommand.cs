using System.Runtime.Serialization;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Common.NetResults.System.Writer
{
    [DataContract]
    internal class RestoreCompleteCommand:NetCommand
    {
        [DataMember]
        public ServerId ServerId { get; private set; }

        public RestoreState State { get; private set; }

        public RestoreCompleteCommand(ServerId serverId, RestoreState state)
        {
            ServerId = serverId;
            State = state;
        }
    }
}

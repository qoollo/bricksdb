using System.Collections.Generic;
using System.Runtime.Serialization;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Common.NetResults.System.Writer
{    
    [DataContract]
    internal class RestoreCommand:NetCommand
    {
        [DataMember]
        public RestoreState RestoreState { get; private set; }
        [DataMember]
        public RestoreType Type { get; set; }
        public List<ServerId> DirectServers { get; set; } 

        public RestoreCommand(RestoreState state, RestoreType type = RestoreType.Single)
        {
            RestoreState = state;
            Type = type;
        }
    }
}

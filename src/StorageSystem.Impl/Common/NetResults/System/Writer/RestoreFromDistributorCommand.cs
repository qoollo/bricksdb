using System.Collections.Generic;
using System.Runtime.Serialization;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Common.NetResults.System.Writer
{
    [DataContract]
    internal class RestoreFromDistributorCommand:NetCommand
    {
        [DataMember]
        public RestoreType Type { get; set; }

        [DataMember]
        public RestoreState RestoreState { get; set; }

        [DataMember]
        public List<ServerId> Servers { get; set; } 

        public RestoreFromDistributorCommand(RestoreState state, ServerId server, RestoreType type = RestoreType.Single)
        {
            Type = type;
            RestoreState = state;
            Servers = new List<ServerId> {server};
        }

        public RestoreFromDistributorCommand(RestoreState state, List<ServerId> servers, RestoreType type = RestoreType.Single)
        {
            Type = type;
            RestoreState = state;
            Servers = servers;
        }
    }
}

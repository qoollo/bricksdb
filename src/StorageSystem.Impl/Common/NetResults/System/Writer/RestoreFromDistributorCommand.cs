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
        public RestoreState RestoreState { get; set; }

        [DataMember]
        public ServerId Server { get; set; } 

        public RestoreFromDistributorCommand(RestoreState state = RestoreState.Default, ServerId server = null)
        {
            RestoreState = state;
            Server = server;
        }
    }
}

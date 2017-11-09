using System.Collections.Generic;
using System.Runtime.Serialization;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Common.NetResults.System.Distributor
{
    [DataContract]
    internal class SetRestoreStateCommand:NetCommand
    {
        [DataMember]
        public RestoreState State { get; private set; }

        [DataMember]
        public List<ServerId> Servers { get; private set; }

        [DataMember]
        public WriterUpdateState UpdateState { get; private set; }

        public SetRestoreStateCommand(RestoreState state, List<ServerId> servers, WriterUpdateState updateState)
        {
            State = state;
            Servers = servers;
            UpdateState = updateState;
        }
    }
}

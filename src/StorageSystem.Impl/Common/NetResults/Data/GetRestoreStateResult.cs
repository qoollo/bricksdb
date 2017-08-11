using System.Collections.Generic;
using System.Runtime.Serialization;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Common.NetResults.Data
{
    [DataContract]
    internal class GetRestoreStateResult:SuccessResult
    {
        [DataMember]
        public Dictionary<string, string> FullState;

        [DataMember]
        public RestoreState State { get; private set; }

        [DataMember]
        public List<RestoreServer> RestoreServers { get; private set; }

        public GetRestoreStateResult(RestoreState state, Dictionary<string, string> fullState, List<RestoreServer> restoreServers)
         {
             FullState = fullState;
             State = state;
             RestoreServers = restoreServers;
         }
    }
}

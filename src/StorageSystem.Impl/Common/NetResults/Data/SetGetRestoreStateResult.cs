using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Common.NetResults.Data
{
    [DataContract]
    internal class SetGetRestoreStateResult:SuccessResult
    {
        [DataMember]
        public Dictionary<string, string> FullState;

        [DataMember]
        public RestoreState State { get; private set; }

         public SetGetRestoreStateResult(RestoreState state, Dictionary<string, string> fullState)
         {
             FullState = fullState;
             State = state;
         }
    }
}

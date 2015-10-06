using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Common.NetResults.System.Distributor
{
    [DataContract]
    internal class SetGetRestoreStateCommand
    {
        [DataMember]
        public RestoreState State { get; private set; }

        public SetGetRestoreStateCommand(RestoreState state)
        {
            State = state;
        }
    }
}

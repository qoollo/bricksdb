using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Impl.Common.HashFile;

namespace Qoollo.Impl.Common.NetResults.System.Distributor
{
    [DataContract]
    internal class HashFileUpdateCommand:NetCommand
    {
        [DataMember]
        public List<HashMapRecord> Map { get; private set; }

        public HashFileUpdateCommand(List<HashMapRecord> map)
        {
            Contract.Requires(map != null);
            Map = map;
        }
    }
}

using System.Collections.Generic;
using System.Runtime.Serialization;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.NetInterfaces.Data
{
    [DataContract]
    internal class DistributorDataContainer
    {
        [DataMember]
        public List<ServerId> AllDistributors { get; private set; }

        public DistributorDataContainer(List<ServerId> allDistributors)
        {
            AllDistributors = allDistributors;
        }
    }
}

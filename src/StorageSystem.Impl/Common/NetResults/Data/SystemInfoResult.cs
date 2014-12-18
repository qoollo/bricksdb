using System.Runtime.Serialization;
using Qoollo.Impl.NetInterfaces.Data;

namespace Qoollo.Impl.Common.NetResults.Data
{
    [DataContract]
    internal class SystemInfoResult:SuccessResult
    {
        [DataMember]
        public DistributorDataContainer DataContainer { get;  set; }

        public SystemInfoResult(DistributorDataContainer dataContainer)
        {
            DataContainer = dataContainer;
        }
    }
}

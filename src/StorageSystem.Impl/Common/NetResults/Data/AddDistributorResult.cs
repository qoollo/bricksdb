using System.Runtime.Serialization;

namespace Qoollo.Impl.Common.NetResults.Data
{
    [DataContract]
    internal class AddDistributorResult:SuccessResult
    {
        [DataMember]
        public string Hash { get; private set; }

        public AddDistributorResult(string hash)
        {
            Hash = hash;
        }
    }
}

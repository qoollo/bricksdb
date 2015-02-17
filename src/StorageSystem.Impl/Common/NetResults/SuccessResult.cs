using System.Runtime.Serialization;

namespace Qoollo.Impl.Common.NetResults
{
    [DataContract]
    public class SuccessResult:RemoteResult
    {
        public SuccessResult() : base(false, "")
        {
        }
    }
}

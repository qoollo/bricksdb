using System.Runtime.Serialization;

namespace Qoollo.Impl.Common.NetResults
{
    [DataContract]
    internal class SuccessResult:RemoteResult
    {
        public SuccessResult() : base(false, "")
        {
        }
    }
}

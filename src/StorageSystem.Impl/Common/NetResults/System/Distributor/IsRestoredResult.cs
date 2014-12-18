using System.Runtime.Serialization;

namespace Qoollo.Impl.Common.NetResults.System.Distributor
{
    [DataContract]
    internal class IsRestoredResult:SuccessResult
    {
        [DataMember]
        public bool IsRestored { get; private set; }

        public IsRestoredResult(bool isRestored)
        {
            IsRestored = isRestored;
        }
    }
}

using System.Runtime.Serialization;

namespace Qoollo.Impl.Common.NetResults.Event
{
    [DataContract]
    internal class InitiatorNotAvailableResult:FailNetResult
    {
        public InitiatorNotAvailableResult() : base("Remote server is unavailable")
        {
        }
    }
}

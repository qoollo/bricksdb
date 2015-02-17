using System.Runtime.Serialization;

namespace Qoollo.Impl.Common.NetResults
{
    [DataContract]    
    public class FailNetResult:RemoteResult
    {
        public FailNetResult(string description) : base(true, description)
        {
        }
    }
}

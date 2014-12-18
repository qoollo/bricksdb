using System.Runtime.Serialization;

namespace Qoollo.Impl.Common.NetResults
{
    [DataContract]    
    internal class FailNetResult:RemoteResult
    {
        public FailNetResult(string description) : base(true, description)
        {
        }
    }
}

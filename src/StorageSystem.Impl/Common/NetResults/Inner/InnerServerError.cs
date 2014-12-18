using System.Runtime.Serialization;

namespace Qoollo.Impl.Common.NetResults.Inner
{
    [DataContract]
    internal class InnerServerError : InnerFailResult
    {
        public InnerServerError(string message) : base(message)
        {
        }

        public InnerServerError(RemoteResult result) : base(result.Description)
        {
        }
    }
}

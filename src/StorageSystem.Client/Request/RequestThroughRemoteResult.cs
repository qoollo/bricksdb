using Qoollo.Impl.Common;

namespace Qoollo.Client.Request
{
    internal class RequestThroughRemoteResult:IRequestDescription
    {
        private readonly RemoteResult _result;

        public RequestThroughRemoteResult(RemoteResult result)
        {
            _result = result;
        }

        public bool IsError { get { return _result.IsError; } }
        public string ErrorDescription { get { return _result.Description; } }

        public RequestState State
        {
            get
            {
                return RequestHelper.GetStatefromRemoteResult(_result);
            }
        }

        public string DistributorHash {get { return ""; }}
        public string CacheKey { get { return ""; }}
    }
}

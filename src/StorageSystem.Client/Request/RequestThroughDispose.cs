namespace Qoollo.Client.Request
{
    internal class RequestThroughDispose : IRequestDescription
    {
        public bool IsError
        {
            get { return true; }
        }

        public string ErrorDescription
        {
            get { return "System disposed"; }
        }

        public RequestState State
        {
            get { return RequestState.Error; }
        }

        public string DistributorHash
        {
            get { return ""; }
        }

        public string CacheKey
        {
            get { return ""; }
        }
    }
}

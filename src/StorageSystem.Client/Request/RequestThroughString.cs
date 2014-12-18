namespace Qoollo.Client.Request
{
    internal class RequestThroughString : IRequestDescription
    {
        private string _string;

        public RequestThroughString(string s)
        {
            _string = s;
        }

        public bool IsError
        {
            get { return RequestHelper.IsErrorString(_string); }
        }

        public string ErrorDescription
        {
            get
            {
                if (IsError)
                    return _string;
                return "";
            }
        }

        public RequestState State
        {
            get
            {
                if (IsError)
                    return RequestState.Error;
                return RequestState.Complete;
            }
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

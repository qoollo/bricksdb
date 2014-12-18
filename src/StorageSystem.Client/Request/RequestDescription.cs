using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.TransactionTypes;

namespace Qoollo.Client.Request
{
    public class RequestDescription
    {
        private IRequestDescription _request;
        private bool _dataNotFound;

        public RequestDescription(UserTransaction userTransaction)
        {
            _dataNotFound = false;
            _request = new RequestThroughTransaction(userTransaction);
        }

        public RequestDescription(RemoteResult result)
        {
            _request = new RequestThroughRemoteResult(result);
        }

        public RequestDescription(string result)
        {
            _request = new RequestThroughString(result);
        }

        public RequestDescription()
        {
            _request = new RequestThroughDispose();
        }

        public bool IsError
        {
            get { return _request.IsError; }
        }

        public string ErrorDescription
        {
            get { return _request.ErrorDescription; }
        }

        public RequestState State
        {
            get
            {
                if(_dataNotFound)
                    return RequestState.DataNotFound;
                return _request.State;
            }
        }

        internal string DistributorHash
        {
            get { return _request.DistributorHash; }
        }

        internal string CacheKey
        {
            get { return _request.CacheKey; }
        }

        internal void DataNotFound()
        {
            _dataNotFound = true;
        }

        public override string ToString()
        {
            if (State == RequestState.Complete)
                return State.ToString();
            return string.Format("State: {0}, Description: {1}", State, ErrorDescription);
        }
    }
}

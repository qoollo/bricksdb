using System.Diagnostics.Contracts;
using Qoollo.Impl.Common.Data.TransactionTypes;

namespace Qoollo.Client.Request
{
    internal class RequestThroughTransaction : IRequestDescription
    {
        private UserTransaction _userTransaction;

        public RequestThroughTransaction(UserTransaction userTransaction)
        {
            _userTransaction = userTransaction;            
        }


        public bool IsError{get { return _userTransaction.IsError; }}

        public string ErrorDescription{get { return _userTransaction.ErrorDescription; }}

        public RequestState State
        {
            get
            {
                var res = (RequestState)_userTransaction.State;
                Contract.Assert(res.ToString()== _userTransaction.State.ToString());
                return res;
            }}

        public string DistributorHash { get { return _userTransaction.DistributorHash; } }

        public string CacheKey { get { return _userTransaction.CacheKey; } }
    }
}

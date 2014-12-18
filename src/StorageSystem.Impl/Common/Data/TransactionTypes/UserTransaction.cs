using System.Diagnostics.Contracts;
using System.Runtime.Serialization;
using System.ServiceModel;
using Qoollo.Impl.Common.Data.Support;

namespace Qoollo.Impl.Common.Data.TransactionTypes
{
    [DataContract]
    public class UserTransaction
    {
        [DataMember]
        public string DistributorHash { get; private set; }

        [DataMember]
        public bool IsError { get; private set; }

        [DataMember]
        public string ErrorDescription { get; private set; }

        [DataMember]
        private string _cacheKey;
        public string CacheKey { get { return _cacheKey; } }

        [DataMember]
        public TransactionState State { get; set; }

        public UserTransaction(string distributorHash)
        {
            Contract.Requires(distributorHash!="");
            DistributorHash = distributorHash;
            IsError = false;
            ErrorDescription = "";
        }

        public UserTransaction(UserTransaction userTransaction)
        {
            DistributorHash = userTransaction.DistributorHash;
            IsError = userTransaction.IsError;
            ErrorDescription = userTransaction.ErrorDescription;
            _cacheKey = userTransaction.CacheKey;
            State = userTransaction.State;
        }

        /// <summary>
        /// Change state to Error
        /// </summary>
        [OperationContract]
        public void SetError()
        {
            State = TransactionState.Error;
            IsError = true;
        }

        [OperationContract]
        public void AddErrorDescription(string error)
        {
            ErrorDescription += error + "; ";
        }

        public void SetCacheKey(string cacheKey)
        {
            _cacheKey = cacheKey;
        }

        public void ClearError()
        {
            IsError = false;
            ErrorDescription = "";
        }
    }
}

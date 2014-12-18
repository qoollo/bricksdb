using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading.Tasks;
using Libs.PerformanceCounters;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Common.Data.TransactionTypes
{
    [DataContract]
    public class Transaction
    {
        [DataMember] 
        private UserTransaction _userTransaction;

        public UserTransaction UserTransaction
        {
            get { return _userTransaction; }
        }

        [DataMember] 
        private SystemTransaction _systemTransaction;

        public SystemTransaction SystemTransaction
        {
            get { return _systemTransaction; }
        }

        #region Data

        #region Main

        public string CacheKey
        {
            get { return _userTransaction.CacheKey; }
        }

        public string EventHash
        {
            get { return _systemTransaction.EventHash; }
        }

        public string DistributorHash
        {
            get { return _userTransaction.DistributorHash; }
        }

        public bool IsError
        {
            get { return _userTransaction.IsError || _systemTransaction.IsError; }
        }

        public string ErrorDescription
        {
            get { return _userTransaction.ErrorDescription; }
        }

        public TransactionState State
        {
            get { return _systemTransaction.State; }
        }

        public string CustomOperationField
        {
            get { return _systemTransaction.CustomOperationField; }
            set { _systemTransaction.CustomOperationField = value; }
        }

        public int TransactionAnswersCount
        {
            get { return _systemTransaction.TransactionAnswersCount; }
        }

        public OperationName OperationName
        {
            get { return _systemTransaction.OperationName; }
            set
            {
                _systemTransaction.OperationName = value;
                _userTransaction.SetCacheKey(_systemTransaction.CacheKey);
            }
        }

        public OperationType OperationType
        {
            get { return _systemTransaction.OperationType; }
            set { _systemTransaction.OperationType = value; }
        }

        public ServerId Distributor
        {
            get { return _systemTransaction.Distributor; }
            set { _systemTransaction.Distributor = value; }
        }

        public List<ServerId> Destination
        {
            get { return _systemTransaction.Destination; }
            set { _systemTransaction.Destination = value; }
        }

        public ServerId ProxyServerId
        {
            get { return _systemTransaction.ProxyServerId; }
            set { _systemTransaction.ProxyServerId = value; }
        }

        public string TableName
        {
            get { return _systemTransaction.TableName; }
            set { _systemTransaction.TableName = value; }
        }

        public bool HashFromValue
        {
            get { return _systemTransaction.HashFromValue; }
            set { _systemTransaction.HashFromValue = value; }
        }

        #endregion

        #region Support

        public TaskCompletionSource<UserTransaction> UserSupportCallback
        {
            get { return _systemTransaction.UserSupportCallback; }
            set { _systemTransaction.UserSupportCallback = value; }
        }

        public TaskCompletionSource<InnerData> InnerSupportCallback
        {
            get { return _systemTransaction.InnerSupportCallback; }
            set { _systemTransaction.InnerSupportCallback = value; }
        }

        public TimeCounterTimer PerfTimer
        {
            get { return _systemTransaction.PerfTimer; }
            set { _systemTransaction.PerfTimer = value; }
        }

        public bool IsNeedAllServes
        {
            get { return _systemTransaction.IsNeedAllServes; }
            set { _systemTransaction.IsNeedAllServes = value; }
        }

        #endregion

        #endregion

        public Transaction(string eventHash, string distributorHash)
        {
            _userTransaction = new UserTransaction(distributorHash);
            _systemTransaction = new SystemTransaction(eventHash);
        }

        public Transaction(Transaction transaction)
        {
            _userTransaction = new UserTransaction(transaction.UserTransaction);
            _systemTransaction = new SystemTransaction(transaction.SystemTransaction);

            _userTransaction.SetCacheKey(transaction.UserTransaction.CacheKey);
        }

        public Transaction(UserTransaction transaction)
        {
            _userTransaction = new UserTransaction(transaction);
            _systemTransaction = new SystemTransaction("-1");
        }

        #region Methods

        #region Change State

        /// <summary>
        /// Change state to Error
        /// </summary>
        [OperationContract]
        public void SetError()
        {
            _userTransaction.SetError();
            _systemTransaction.SetError();
        }

        /// <summary>
        /// Change state to TransactionInProcess
        /// </summary>
        [OperationContract]
        public void StartTransaction()
        {
            _userTransaction.State = TransactionState.TransactionInProcess;
            _systemTransaction.StartTransaction();
        }

        [OperationContract]
        public void Complete()
        {
            _userTransaction.State = TransactionState.Complete;
            _systemTransaction.Complete();
        }

        [OperationContract]
        public void DoesNotExist()
        {
            _userTransaction.State = TransactionState.DontExist;
            _systemTransaction.DoesNotExist();
        }

        #endregion

        public void IncreaseTransactionAnswersCount()
        {
            _systemTransaction.IncreaseTransactionAnswersCount();
        }

        [OperationContract]
        public void AddErrorDescription(string error)
        {
            _userTransaction.AddErrorDescription(error);
        }

        public void ClearError()
        {
            _userTransaction.ClearError();
            _systemTransaction.ClearError();
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading.Tasks;
using Qoollo.PerformanceCounters;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Common.Data.TransactionTypes
{
    [DataContract]
    public class SystemTransaction
    {
        #region Data

        #region Main

        [DataMember]
        public string EventHash { get; private set; }

        private TransactionState _state;

        [DataMember]
        public TransactionState State
        {
            get
            {
                if (IsError)
                    return TransactionState.Error;
                return _state;
            }
            private set { _state = value; }
        }        

        [DataMember]
        public OperationName OperationName { get; set; }

        [DataMember]
        public OperationType OperationType { get; set; }

        [DataMember]
        public bool IsError { get; private set; }

        [DataMember]        
        public ServerId Distributor { get; set; }        

        [DataMember]
        public string CustomOperationField { get; set; }

        [DataMember]
        public ServerId ProxyServerId { get; set; }

        [DataMember]
        public string TableName { get; set; }

        [DataMember]
        public bool HashFromValue { get; set; }

        #endregion

        #region Support

        [DataMember] 
        private DateTimeOffset _uniqueTime;

        public TaskCompletionSource<UserTransaction> UserSupportCallback { get; set; }

        public TaskCompletionSource<InnerData> InnerSupportCallback { get; set; }
 
        public int TransactionAnswersCount { get; private set; }

        public string CacheKey { get { return EventHash + OperationName + _uniqueTime; } }

        /// <summary>
        /// Servers for where data store
        /// </summary>        
        public List<ServerId> Destination { get; set; }        

        public TimeCounterTimer PerfTimer { get; set; }

        /// <summary>
        /// Is need find data on all servers        
        /// </summary>
        public bool IsNeedAllServes { get; set; }

        #endregion

        #endregion

        public SystemTransaction(string eventHash)
        {
            Contract.Requires(eventHash != "");
            EventHash = eventHash;
            IsError = false;
            State = TransactionState.InProcess;
            TransactionAnswersCount = 0;
            _uniqueTime = DateTimeOffset.Now;
            HashFromValue = false;
        }

        public SystemTransaction(SystemTransaction systemTransaction)
        {
            EventHash = systemTransaction.EventHash;
            State = systemTransaction.State;
            OperationName = systemTransaction.OperationName;
            OperationType = systemTransaction.OperationType;
            IsError = systemTransaction.IsError;
            TransactionAnswersCount = systemTransaction.TransactionAnswersCount;
            PerfTimer = systemTransaction.PerfTimer;
            Distributor = systemTransaction.Distributor;
            _uniqueTime = systemTransaction._uniqueTime;
            CustomOperationField = systemTransaction.CustomOperationField;
            TableName = systemTransaction.TableName;
            HashFromValue = systemTransaction.HashFromValue;
        }

        #region Change State

        /// <summary>
        /// Change state to Error
        /// </summary>
        [OperationContract]
        public void SetError()
        {
            State = TransactionState.Error;
            IsError = true;
        }

        /// <summary>
        /// Change state to TransactionInProcess
        /// </summary>
        [OperationContract]
        public void StartTransaction()
        {
            _state = TransactionState.TransactionInProcess;
        }

        [OperationContract]
        public void Complete()
        {
            if (State != TransactionState.Error)
                State = TransactionState.Complete;
        }

        [OperationContract]
        public void DoesNotExist()
        {
            State = TransactionState.DontExist;
        }

        public void ClearError()
        {
            IsError = false;
        }

        #endregion

        #region Support

        public void IncreaseTransactionAnswersCount()
        {
            TransactionAnswersCount++;
        }

        #endregion
    }
}

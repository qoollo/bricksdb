using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.DistributorModules.Caches;
using Qoollo.Impl.DistributorModules.Transaction;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Queue;

namespace Qoollo.Impl.DistributorModules
{
    internal class MainLogicModule : ControlModule
    {
        private readonly DistributorTimeoutCache _cache;
        private readonly DistributorModule _distributor;
        private readonly TransactionModule _transaction;
        private readonly GlobalQueueInner _queue;

        public MainLogicModule(DistributorTimeoutCache cache, DistributorModule distributor,
            TransactionModule transaction)
        {
            Contract.Requires(cache != null);
            Contract.Requires(distributor != null);
            Contract.Requires(transaction != null);
            _cache = cache;
            _distributor = distributor;
            _transaction = transaction;
            _queue = GlobalQueue.Queue;
        }

        public override void Start()
        {
            _queue.TransactionQueue.Registrate(TransactionAnswerIncome);
            Logger.Logger.Instance.Debug("Mainlogic: start");
        }

        #region Public

        private bool GetCountServers(InnerData data)
        {
            if (data.Transaction.OperationName == OperationName.Read)
            {
                if (data.Transaction.HashFromValue)
                    return true;
                return _distributor.IsSomethingHappendInSystem();
            }
            return false;
        }

        public void ProcessWithData(InnerData data, TransactionExecutor executor)
        {
            Logger.Logger.Instance.Debug(string.Format("Mainlogic: process data = {0}", data.Transaction.DataHash));

            var dest = _distributor.GetDestination(data, GetCountServers(data));
            if (dest == null)
            {
                Logger.Logger.Instance.Debug(string.Format("Mainlogic: dont found destination, process data = {0}",
                    data.Transaction.DataHash));
                data.Transaction.SetError();
                data.Transaction.AddErrorDescription(Errors.NotAvailableServersForWrite);
            }
            else
                data.Transaction.Destination = new List<ServerId>(dest);

            if (data.Transaction.OperationName != OperationName.Read)
                AddDataToCache(data);

            if (!data.Transaction.IsError)
            {
                data.Transaction.Distributor = _distributor.LocalForDb;

                _transaction.ProcessSyncWithExecutor(data, executor);

                if (data.Transaction.IsError)
                {
                    if (data.Transaction.OperationName != OperationName.Read)
                        UpdateDataToCache(data);
                    if (data.Transaction.OperationType == OperationType.Sync)
                        ProcessSyncTransaction(data.Transaction);
                }
            }
            else
            {
                Logger.Logger.Instance.Debug(string.Format("Mainlogic: process data = {0}, result = {1}",
                    data.Transaction.DataHash, !data.Transaction.IsError));

                if (data.Transaction.OperationName != OperationName.Read)
                    UpdateDataToCache(data);

                if (data.Transaction.OperationType == OperationType.Sync)
                    ProcessSyncTransaction(data.Transaction);


                data.Transaction.PerfTimer.Complete();

                PerfCounters.DistributorCounters.Instance.ProcessPerSec.OperationFinished();
                PerfCounters.DistributorCounters.Instance.TransactionFailCount.Increment();
            }
        }

        public void DataTimeout(InnerData data)
        {
            Logger.Logger.Instance.ErrorFormat("Operation timeout with key {0}", data.Transaction.CacheKey);

            data.Transaction.SetError();
            data.Transaction.AddErrorDescription(Errors.TimeoutExpired);

            _cache.Update(data.Transaction.CacheKey, data);
        }

        public void TransactionAnswerIncome(Common.Data.TransactionTypes.Transaction transaction)
        {
            var item = _cache.Get(transaction.CacheKey);

            //Так как обработка однопоточная, то если элемента нет в кеше
            // значит, что либо кеш обновляется(только в случае ошибки), либо 
            // элемента просто нет в кеше(тоже ошибка)
            if (item == null)
                return;

            if (transaction.IsError || item.Transaction.IsError)
            {
                AddErrorAndUpdate(item, transaction.ErrorDescription);                
            }

            //if (transaction.IsError && !item.IsError)
            //    _transaction.RollbackTransaction(item);

            item.Transaction.IncreaseTransactionAnswersCount();

            if (item.Transaction.TransactionAnswersCount > _transaction.CountReplics)
            {
                AddErrorAndUpdate(item, Errors.TransactionCountAnswersError);
                return;
            }

            if (item.Transaction.TransactionAnswersCount == _transaction.CountReplics)
                FinishTransaction(item);
        }

        public UserTransaction GetTransactionState(UserTransaction transaction)
        {
            var value = _cache.Get(transaction.CacheKey);
            if (value == null)
            {
                var ret = new Common.Data.TransactionTypes.Transaction("", "");
                ret.DoesNotExist();
                return ret.UserTransaction;

            }
            return value.Transaction.UserTransaction;
        }

        #endregion

        #region Private

        private void FinishTransaction(InnerData data)
        {
            data.Transaction.Complete();

            Logger.Logger.Instance.Trace(string.Format("Mainlogic: process data = {0}, result = {1}",
                data.Transaction.CacheKey, !data.Transaction.IsError));

            if (data.Transaction.OperationType == OperationType.Sync)
                ProcessSyncTransaction(data.Transaction);
            else
                _cache.Update(data.Transaction.CacheKey, data);

            data.Transaction.PerfTimer.Complete();
            PerfCounters.DistributorCounters.Instance.ProcessPerSec.OperationFinished();
        }

        private void AddErrorAndUpdate(InnerData data, string error)
        {
            data.Transaction.SetError();
            data.Transaction.AddErrorDescription(error);

            if (data.Transaction.OperationType == OperationType.Sync)
                ProcessSyncTransaction(data.Transaction);
            else
                _cache.Update(data.Transaction.CacheKey, data);
        }

        private void ProcessSyncTransaction(Common.Data.TransactionTypes.Transaction item)
        {
            if (item.OperationName != OperationName.Read)
            {
                _cache.Remove(item.CacheKey);
                _queue.DistributorTransactionCallbackQueue.Add(item);
            }
        }

        private void AddDataToCache(InnerData data)
        {
            var item = new InnerData(data.Transaction) {Key = data.Key};
            _cache.AddToCache(data.Transaction.CacheKey, item);
        }

        private void UpdateDataToCache(InnerData data)
        {
            var item = new InnerData(data.Transaction) { Key = data.Key };
            _cache.Update(data.Transaction.CacheKey, item);
        }

        #endregion
    }
}

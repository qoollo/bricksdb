using System.Collections.Generic;
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
    /// <summary>
    /// Модуль, в котором должен реализовывать логика
    /// </summary>
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
            Logger.Logger.Instance.Debug(string.Format("Mainlogic: process data = {0}", data.Transaction.EventHash));

            var dest = _distributor.GetDestination(data, GetCountServers(data));
            if (dest == null)
            {
                Logger.Logger.Instance.Debug(string.Format("Mainlogic: dont found destination, process data = {0}",
                    data.Transaction.EventHash));
                data.Transaction.SetError();
                data.Transaction.AddErrorDescription(Errors.NotAvailableServersForWrite);
            }
            else
            {
                data.Transaction.Destination = new List<ServerId>(dest);
            }
            if (data.Transaction.OperationName != OperationName.Read)
                _cache.AddToCache(data.Transaction.CacheKey, data.Transaction);
            if (!data.Transaction.IsError)
            {
                data.Transaction.Distributor = _distributor.LocalForDb;

                _transaction.ProcessSyncWithExecutor(data, executor);

                if (data.Transaction.IsError)
                {
                    if (data.Transaction.OperationName != OperationName.Read)
                        _cache.Update(data.Transaction.CacheKey, data.Transaction);
                    if (data.Transaction.OperationType == OperationType.Sync)
                        ProcessSyncTransaction(data.Transaction);
                }
            }
            else
            {
                Logger.Logger.Instance.Debug(string.Format("Mainlogic: process data = {0}, result = {1}",
                    data.Transaction.EventHash, !data.Transaction.IsError));

                if (data.Transaction.OperationName != OperationName.Read)
                    _cache.Update(data.Transaction.CacheKey, data.Transaction);

                if (data.Transaction.OperationType == OperationType.Sync)
                    ProcessSyncTransaction(data.Transaction);


                data.Transaction.PerfTimer.Complete();

                PerfCounters.DistributorCounters.Instance.ProcessPerSec.OperationFinished();
                PerfCounters.DistributorCounters.Instance.TransactionFailCount.Increment();
            }
        }

        public void DataTimeout(Common.Data.TransactionTypes.Transaction transaction)
        {
            Logger.Logger.Instance.ErrorFormat("Operation timeout with key {0}", transaction.CacheKey);

            transaction.SetError();
            transaction.AddErrorDescription(Errors.TimeoutExpired);

            _cache.Update(transaction.CacheKey, transaction);
        }

        public void TransactionAnswerIncome(Common.Data.TransactionTypes.Transaction transaction)
        {
            var item = _cache.Get(transaction.CacheKey);

            //Так как обработка однопоточная, то если элемента нет в кеше
            // значит, что либо кеш обновляется(только в случае ошибки), либо 
            // элемента просто нет в кеше(тоже ошибка)
            if (item == null)
                return;

            if (item.IsError)
            {
                if (transaction.IsError)
                    AddErrorAndUpdate(item, transaction.ErrorDescription);
                return;
            }

            if (transaction.IsError)
            {
                AddErrorAndUpdate(item, transaction.ErrorDescription);                
            }

            item.IncreaseTransactionAnswersCount();

            if (item.TransactionAnswersCount > _transaction.CountReplics)
            {
                AddErrorAndUpdate(item, Errors.TransactionCountAnswersError);
                return;
            }

            if (item.TransactionAnswersCount == _transaction.CountReplics)
                FinishTransaction(item);
        }

        public UserTransaction GetTransactionState(UserTransaction transaction)
        {
            var value = _cache.Get(transaction.CacheKey);
            if (value == null)
            {
                var ret = new Common.Data.TransactionTypes.Transaction("", "");
                //todo 
                ret.DoesNotExist();
                return ret.UserTransaction;

            }
            return value.UserTransaction;
        }

        #endregion

        #region Private

        private void FinishTransaction(Common.Data.TransactionTypes.Transaction item)
        {
            item.Complete();

            Logger.Logger.Instance.Trace(string.Format("Mainlogic: process data = {0}, result = {1}",
                item.EventHash, !item.IsError));

            if (item.OperationType == OperationType.Sync)
                ProcessSyncTransaction(item);
            else
                _cache.Update(item.CacheKey, item);

            item.PerfTimer.Complete();
            PerfCounters.DistributorCounters.Instance.ProcessPerSec.OperationFinished();
        }

        private void AddErrorAndUpdate(Common.Data.TransactionTypes.Transaction item, string error)
        {
            item.SetError();
            item.AddErrorDescription(error);

            if (item.OperationType == OperationType.Sync)
                ProcessSyncTransaction(item);
            else
                _cache.Update(item.CacheKey, item);
        }

        private void ProcessSyncTransaction(Common.Data.TransactionTypes.Transaction item)
        {
            if (item.OperationName != OperationName.Read)
            {
                _cache.Remove(item.CacheKey);
                _queue.DistributorTransactionCallbackQueue.Add(item);
            }
        }

        #endregion
    }
}

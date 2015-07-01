using System;
using System.Diagnostics.Contracts;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.NetResults.System.Distributor;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DistributorModules.Caches;
using Qoollo.Impl.DistributorModules.DistributorNet.Interfaces;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Turbo.ObjectPools;

namespace Qoollo.Impl.DistributorModules.Transaction
{
    internal class TransactionModule : ControlModule
    {
        public TransactionModule(INetModule net, TransactionConfiguration transactionConfiguration,
            int countReplics, DistributorTimeoutCache cache)
        {
            Contract.Requires(net != null);
            Contract.Requires(transactionConfiguration != null);
            Contract.Requires(countReplics>0);            
            Contract.Requires(cache != null);                        

            _transactionPool = new TransactionPool(transactionConfiguration.ElementsCount, net, countReplics);
            _countReplics = countReplics;
            _net = net;
            _cache = cache;
            _queue = GlobalQueue.Queue;

            _cache.DataTimeout += DataTimeout;
        }

        private readonly int _countReplics;
        private readonly DistributorTimeoutCache _cache;
        private readonly TransactionPool _transactionPool;
        private readonly INetModule _net;
        private readonly GlobalQueueInner _queue;

        #region ControlModule

        public override void Start()
        {
            _queue.TransactionQueue.Registrate(TransactionAnswerIncome);
            _transactionPool.FillPoolUpTo(_transactionPool.MaxElementCount);
        }

        public void ProcessWithExecutor(InnerData data, TransactionExecutor executor)
        {
            if (data.Transaction.OperationName == OperationName.Read)
                Read(data, executor);
            else
                ExecuteTransaction(data, executor);
        }

        private void ExecuteTransaction(InnerData data, TransactionExecutor executor)
        {
            Logger.Logger.Instance.Debug(string.Format("Transaction process data = {0}", data.Transaction.DataHash));

            data.Transaction.StartTransaction();

            executor.Commit(data);
        }

        public RentedElementMonitor<TransactionExecutor> Rent()
        {
            return _transactionPool.Rent();
        }

        private void Rollback(InnerData data)
        {
            try
            {
                foreach (var server in data.Transaction.Destination)
                {
                    _net.Rollback(server, data);
                }
            }
            catch (Exception e)
            {
            }
        }

        #endregion

        #region Read operation

        private void Read(InnerData data, TransactionExecutor executor)
        {
            var result = executor.ReadSimple(data);
            ProcessReadResult(data, result, data.Transaction.ProxyServerId);
            data.Transaction.PerfTimer.Complete();
            PerfCounters.DistributorCounters.Instance.ProcessPerSec.OperationFinished();
        }

        private void ProcessReadResult(InnerData data, InnerData result, ServerId proxyServerId)
        {
            if (result == null)
            {
                result = data;
                result.Data = null;

                result.Transaction.SetError();
                result.Transaction.AddErrorDescription(Errors.ServerIsNotAvailable);
            }

            if(!result.Transaction.IsError)
                result.Transaction.Complete();

            _net.ASendToProxy(proxyServerId, new ReadOperationCompleteCommand(result));
        }

        #endregion

        public void TransactionAnswerIncome(Common.Data.TransactionTypes.Transaction transaction)
        {
            var item = _cache.Get(transaction.CacheKey);

            if (item == null)
                return;

            using (item.DistributorData.GetLock())
            {
                if (transaction.IsError && !item.DistributorData.IsRollbackSended)
                {
                    item.DistributorData.SendRollback();
                    Rollback(item);
                }

                if (transaction.ErrorDescription != "" || item.Transaction.IsError)
                {
                    AddErrorAndUpdate(item, transaction.ErrorDescription);
                }

                item.Transaction.IncreaseTransactionAnswersCount();

                if (item.Transaction.TransactionAnswersCount > _countReplics)
                {
                    AddErrorAndUpdate(item, Errors.TransactionCountAnswersError);
                    return;
                }

                if (item.Transaction.TransactionAnswersCount == _countReplics)
                    FinishTransaction(item);
            }
          
        }

        public void ProcessSyncTransaction(InnerData data)
        {
            if (data.Transaction.OperationName != OperationName.Read && !data.DistributorData.IsSyncAnswerSended)
            {
                data.DistributorData.SendSyncAnswer();
                _cache.Remove(data.Transaction.CacheKey);
                _queue.DistributorTransactionCallbackQueue.Add(data.Transaction);
            }
        }

        private void DataTimeout(InnerData data)
        {
            Logger.Logger.Instance.ErrorFormat("Operation timeout with key {0}", data.Transaction.CacheKey);

            data.Transaction.SetError();
            data.Transaction.AddErrorDescription(Errors.TimeoutExpired);

            _cache.Update(data.Transaction.CacheKey, data);
        }

        private void FinishTransaction(InnerData data)
        {
            data.Transaction.Complete();

            Logger.Logger.Instance.Trace(string.Format("Mainlogic: process data = {0}, result = {1}",
                data.Transaction.CacheKey, !data.Transaction.IsError));

            if (data.Transaction.OperationType == OperationType.Sync)
                ProcessSyncTransaction(data);
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
                ProcessSyncTransaction(data);
            else
                _cache.Update(data.Transaction.CacheKey, data);
        }

        protected override void Dispose(bool isUserCall)
        {
            _transactionPool.Dispose();
            base.Dispose(isUserCall);
        }
    }
}

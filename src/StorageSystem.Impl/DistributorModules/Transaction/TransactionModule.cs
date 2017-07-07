using System;
using System.Diagnostics.Contracts;
using Ninject;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.NetResults;
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
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        public TransactionModule(StandardKernel kernel, INetModule net, TransactionConfiguration transactionConfiguration,
            int countReplics, DistributorTimeoutCache cache)
            :base(kernel)
        {
            Contract.Requires(net != null);
            Contract.Requires(transactionConfiguration != null);
            Contract.Requires(countReplics>0);            
            Contract.Requires(cache != null);

            _queue = kernel.Get<IGlobalQueue>();

            _transactionPool = new TransactionPool(kernel, transactionConfiguration.ElementsCount, net, countReplics);
            _countReplics = countReplics;
            _net = net;
            _cache = cache;
            _cache.DataTimeout += DataTimeout;
        }

        private readonly int _countReplics;
        private readonly DistributorTimeoutCache _cache;
        private readonly TransactionPool _transactionPool;
        private readonly INetModule _net;
        private readonly IGlobalQueue _queue;

        #region ControlModule

        public override void Start()
        {            
            RegistrateAsync<Common.Data.TransactionTypes.Transaction, Common.Data.TransactionTypes.Transaction,
                RemoteResult>(_queue.TransactionQueue, TransactionAnswerIncome,
                    () => new SuccessResult());
            _transactionPool.FillPoolUpTo(_transactionPool.MaxElementCount);

            StartAsync();
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
            data.Transaction.StartTransaction();
            executor.Commit(data);
        }

        public RentedElementMonitor<TransactionExecutor> Rent()
        {
            return _transactionPool.Rent();
        }

        private void Rollback(InnerData data)
        {
            PerfCounters.DistributorCounters.Instance.TransactionFailCount.Increment();
            try
            {
                foreach (var server in data.DistributorData.Destination)
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
            if (data.DistributorData.ExecuteTimer != null)
                data.DistributorData.ExecuteTimer.Value.Complete();
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
                //if (transaction.ErrorDescription != "" || item.Transaction.IsError)
                //{
                //    AddErrorAndUpdate(item, transaction.ErrorDescription);
                //}

                item.DistributorData.IncreaseTransactionAnswersCount();

                if (item.DistributorData.TransactionAnswersCount > _countReplics)
                {
                    AddErrorAndUpdate(item, Errors.TransactionCountAnswersError);
                    return;
                }

                if (item.DistributorData.TransactionAnswersCount == _countReplics)
                {
                    if (transaction.IsError && !item.DistributorData.IsRollbackSended)
                    {
                        item.DistributorData.SendRollback();
                        Rollback(item);
                    }
                    FinishTransaction(item);
                }
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
            _logger.ErrorFormat("Operation timeout with key {0}", data.Transaction.OperationName);

            data.Transaction.SetError();
            data.Transaction.AddErrorDescription(Errors.TimeoutExpired);

            _cache.Update(data.Transaction.CacheKey, data);
        }

        private void FinishTransaction(InnerData data)
        {
            data.Transaction.Complete();

            if (_logger.IsInfoEnabled)
                _logger.Trace(
                    $"Mainlogic: process data = {data.Transaction.OperationName}, result = {!data.Transaction.IsError} {data.Transaction.ErrorDescription}");

            if (data.Transaction.OperationType == OperationType.Sync)
                ProcessSyncTransaction(data);
            else
                _cache.Update(data.Transaction.CacheKey, data);

            data.Transaction.PerfTimer.Complete();

            if (data.DistributorData.ExecuteTimer != null)
                data.DistributorData.ExecuteTimer.Value.Complete();

            PerfCounters.DistributorCounters.Instance.ProcessPerSec.OperationFinished();            
        }

        private void AddErrorAndUpdate(InnerData data, string error)
        {            
            data.Transaction.SetError();
            data.Transaction.AddErrorDescription(error);

            //if (data.Transaction.OperationType == OperationType.Sync)
            //    ProcessSyncTransaction(data);
            //else
                //_cache.Update(data.Transaction.CacheKey, data);

            //if (data.DistributorData.ExecuteTimer != null)
            //    data.DistributorData.ExecuteTimer.Value.Complete();
        }

        protected override void Dispose(bool isUserCall)
        {
            _transactionPool.Dispose();
            base.Dispose(isUserCall);
        }
    }
}

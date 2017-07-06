using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Ninject;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.DistributorModules.Caches;
using Qoollo.Impl.DistributorModules.Transaction;
using Qoollo.Impl.Modules;

namespace Qoollo.Impl.DistributorModules
{
    internal class MainLogicModule : ControlModule
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        private readonly DistributorModule _distributor;
        private readonly TransactionModule _transaction;
        private readonly DistributorTimeoutCache _cache;

        public MainLogicModule(StandardKernel kernel, DistributorModule distributor, TransactionModule transaction, DistributorTimeoutCache cache)
            :base(kernel)
        {            
            Contract.Requires(distributor != null);
            Contract.Requires(transaction != null);
            Contract.Requires(cache != null);
            _distributor = distributor;
            _transaction = transaction;
            _cache = cache;
        }        

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

        private void CreateTimers(InnerData data)
        {
            switch (data.Transaction.OperationName)
            {
                case OperationName.Create:
                    data.DistributorData.ExecuteTimer = PerfCounters.DistributorCounters.Instance.CreateTimer.StartNew();
                    break;
                case OperationName.Read:
                    data.DistributorData.ExecuteTimer = PerfCounters.DistributorCounters.Instance.ReadTimer.StartNew();
                    break;
            }            
        }

        public void ProcessWithData(InnerData data, TransactionExecutor executor)
        {
            if(_logger.IsTraceEnabled)
                _logger.Debug($"Mainlogic: process data = {data.Transaction.OperationName}");

            data.DistributorData = new DistributorData();
            CreateTimers(data);

            var dest = _distributor.GetDestination(data, GetCountServers(data));
            if (dest == null)
            {
                if(_logger.IsTraceEnabled)
                    _logger.Trace($"Mainlogic: dont found destination, process data = {data.Transaction.DataHash}");
                data.Transaction.SetError();
                data.Transaction.AddErrorDescription(Errors.NotAvailableServersForWrite);
            }
            else
                data.DistributorData.Destination = new List<ServerId>(dest);

            if (data.Transaction.OperationName != OperationName.Read)
                AddToCache(data);

            if (!data.Transaction.IsError)
            {
                data.Transaction.Distributor = _distributor.LocalForDb;
                _transaction.ProcessWithExecutor(data, executor);
            }
            else
            {
                if (_logger.IsWarnEnabled)
                    _logger.Warn(
                        $"Mainlogic: process data = {data.Transaction.DataHash}, result = {data.Transaction.ErrorDescription}");

                WorkWithFailTransaction(data);

                data.Transaction.PerfTimer.Complete();                
            }
        }

        public UserTransaction GetTransactionState(UserTransaction transaction)
        {
            var value = _cache.Get(transaction.CacheKey);
            if (value == null)
            {
                var ret = new Common.Data.TransactionTypes.Transaction("default", "default");
                ret.DoesNotExist();
                return ret.UserTransaction;

            }
            return value.Transaction.UserTransaction;
        }

        private void AddToCache(InnerData data)
        {
            using (data.DistributorData.GetLock())
            {
                _cache.AddDataToCache(data);
            }
        }

        private void UpdateToCache(InnerData data)
        {
            using (data.DistributorData.GetLock())
            {
                _cache.UpdateDataToCache(data);
            }
        }

        private void WorkWithFailTransaction(InnerData data)
        {
            if (data.Transaction.OperationName != OperationName.Read)
                UpdateToCache(data);

            if (data.Transaction.OperationType == OperationType.Sync)
                _transaction.ProcessSyncTransaction(data);
        }
    }
}

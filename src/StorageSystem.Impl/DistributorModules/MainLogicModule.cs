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

namespace Qoollo.Impl.DistributorModules
{
    internal class MainLogicModule : ControlModule
    {
        private readonly DistributorModule _distributor;
        private readonly TransactionModule _transaction;
        private readonly DistributorTimeoutCache _cache;

        public MainLogicModule(DistributorModule distributor, TransactionModule transaction, DistributorTimeoutCache cache)
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
                _cache.AddDataToCache(data);

            if (!data.Transaction.IsError)
            {
                data.Transaction.Distributor = _distributor.LocalForDb;

                _transaction.ProcessSyncWithExecutor(data, executor);

                if (data.Transaction.IsError)
                {
                    if (data.Transaction.OperationName != OperationName.Read)
                        _cache.UpdateDataToCache(data);
                    if (data.Transaction.OperationType == OperationType.Sync)
                        _transaction.RemoveTransaction(data.Transaction);
                }
            }
            else
            {
                Logger.Logger.Instance.Debug(string.Format("Mainlogic: process data = {0}, result = {1}",
                    data.Transaction.DataHash, !data.Transaction.IsError));

                if (data.Transaction.OperationName != OperationName.Read)
                    _cache.UpdateDataToCache(data);

                if (data.Transaction.OperationType == OperationType.Sync)
                    _transaction.RemoveTransaction(data.Transaction);


                data.Transaction.PerfTimer.Complete();

                PerfCounters.DistributorCounters.Instance.ProcessPerSec.OperationFinished();
                PerfCounters.DistributorCounters.Instance.TransactionFailCount.Increment();
            }
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
    }
}

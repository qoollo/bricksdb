using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Proxy.Caches;

namespace Qoollo.Impl.Proxy.Input
{
    internal class ProxyInputModule:ControlModule,IStorageInner
    {
        private readonly string _tableName;
        private readonly ProxyDistributorModule _distributor;
        private readonly IHashCalculater _hashCalculater;
        private readonly AsyncProxyCache _asyncProxyCache;
        private readonly ProxyInputModuleCommon _processTransaction;
        private readonly bool _hashFromValue;

        public ProxyInputModule(string tableName, bool hashFromValue, AsyncProxyCache asyncProxyCache,
            IHashCalculater hashCalculater, ProxyDistributorModule distributor, ProxyInputModuleCommon processTransaction)
        {
            Contract.Requires(distributor != null);
            Contract.Requires(hashCalculater != null);
            Contract.Requires(asyncProxyCache != null);
            Contract.Requires(processTransaction != null);

            _tableName = tableName;
            _distributor = distributor;
            _processTransaction = processTransaction;
            _hashFromValue = hashFromValue;
            _hashCalculater = hashCalculater;
            _asyncProxyCache = asyncProxyCache;
        }

        #region Common Interface

        public UserTransaction Create(object key, object value)
        {
            var hash = _hashFromValue
                ? _hashCalculater.CalculateHashFromValue(value)
                : _hashCalculater.CalculateHashFromKey(key);

            var transaction = _distributor.CreateTransaction(hash);

            transaction.OperationName = OperationName.Create;
            transaction.OperationType = OperationType.Async;

            if (!transaction.IsError)
            {
                CompleteTransaction(key, value, transaction);
                PerfCounters.ProxyCounters.Instance.CreateCount.Increment();
            }            

            return transaction.UserTransaction;
        }

        public UserTransaction Update(object key, object value)
        {
            var hash = _hashFromValue
                ? _hashCalculater.CalculateHashFromValue(value)
                : _hashCalculater.CalculateHashFromKey(key);
            var transaction = _distributor.CreateTransaction(hash);

            transaction.OperationName = OperationName.Update;
            transaction.OperationType = OperationType.Async;

            if (!transaction.IsError)
            {
                CompleteTransaction(key, value, transaction);
                PerfCounters.ProxyCounters.Instance.UpdateCount.Increment();
            }

            return transaction.UserTransaction;
        }

        public UserTransaction Delete(object key)
        {
            var hash = _hashCalculater.CalculateHashFromKey(key);
            var transaction = _distributor.CreateTransaction(hash);

            transaction.OperationName = OperationName.Delete;
            transaction.OperationType = OperationType.Async;

            if (!transaction.IsError)
            {
                CompleteTransactionKeyOnly(key, transaction);
                PerfCounters.ProxyCounters.Instance.DeleteCount.Increment();
            }

            return transaction.UserTransaction;
        }

        public async Task<UserTransaction> CreateSync(object key, object value)
        {
            var hash = _hashFromValue
                ? _hashCalculater.CalculateHashFromValue(value)
                : _hashCalculater.CalculateHashFromKey(key);
            var transaction = _distributor.CreateTransaction(hash);

            transaction.OperationName = OperationName.Create;
            transaction.OperationType = OperationType.Sync;

            if (!transaction.IsError)
            {
                SomeAsyncWork(transaction);
                CompleteTransaction(key, value, transaction);
            }
            else
            {
                var task = new TaskCompletionSource<UserTransaction>();
                task.SetResult(transaction.UserTransaction);

                PerfCounters.ProxyCounters.Instance.CreateCount.Increment();
                return await task.Task;
            }

            var res =  await transaction.UserSupportCallback.Task;
            PerfCounters.ProxyCounters.Instance.CreateCount.Increment();
            return res;
        }

        public async Task<UserTransaction> UpdateSync(object key, object value)
        {
            var hash = _hashFromValue
                ? _hashCalculater.CalculateHashFromValue(value)
                : _hashCalculater.CalculateHashFromKey(key);
            var transaction = _distributor.CreateTransaction(hash);

            transaction.OperationName = OperationName.Update;
            transaction.OperationType = OperationType.Sync;

            if (!transaction.IsError)
            {
                SomeAsyncWork(transaction);
                CompleteTransaction(key, value, transaction);
                PerfCounters.ProxyCounters.Instance.UpdateCount.Increment();
            }
            else
            {
                var task = new TaskCompletionSource<UserTransaction>();
                task.SetResult(transaction.UserTransaction);

                PerfCounters.ProxyCounters.Instance.UpdateCount.Increment();
                return await task.Task;
            }

            var res =  await transaction.UserSupportCallback.Task;
            PerfCounters.ProxyCounters.Instance.UpdateCount.Increment();
            return res;
        }

        public async Task<UserTransaction> DeleteSync(object key)
        {
            var hash = _hashCalculater.CalculateHashFromKey(key);
            var transaction = _distributor.CreateTransaction(hash);

            transaction.OperationName = OperationName.Delete;
            transaction.OperationType = OperationType.Sync;

            if (!transaction.IsError)
            {
                SomeAsyncWork(transaction);
                CompleteTransactionKeyOnly(key, transaction);
            }
            else
            {
                var task = new TaskCompletionSource<UserTransaction>();
                task.SetResult(transaction.UserTransaction);
                PerfCounters.ProxyCounters.Instance.DeleteCount.Increment();
                return await task.Task;
            }

            var res =  await transaction.UserSupportCallback.Task;
            PerfCounters.ProxyCounters.Instance.DeleteCount.Increment();
            return res;
        }

        public object Read(object key, out UserTransaction result)
        {
            var hash = _hashFromValue ? "" : _hashCalculater.CalculateHashFromKey(key);
            var transaction = _distributor.CreateTransaction(hash);

            transaction.OperationName = OperationName.Read;
            transaction.OperationType = OperationType.Sync;

            if (!transaction.IsError)
            {
                SomeAsyncWorkForRead(transaction);
                CompleteTransactionKeyOnly(key, transaction);
            }

            result = transaction.UserTransaction;

            try
            {
                transaction.InnerSupportCallback.Task.Wait();
            }
            catch (Exception)
            {   
                transaction.SetError();
                transaction.AddErrorDescription(Errors.OperationTimeoutException);
                PerfCounters.ProxyCounters.Instance.ReadCount.Increment();
                return null;
            }

            if (transaction.InnerSupportCallback.Task.Result == null)
            {
                transaction.SetError();
                transaction.AddErrorDescription(Errors.OperationTimeoutException);
                PerfCounters.ProxyCounters.Instance.ReadCount.Increment();
                return null;
            }

            result = transaction.InnerSupportCallback.Task.Result.Transaction.UserTransaction;

            var data = transaction.InnerSupportCallback.Task.Result.Data;

            if (data == null)
            {
                PerfCounters.ProxyCounters.Instance.ReadCount.Increment();
                return null;
            }

            PerfCounters.ProxyCounters.Instance.ReadCount.Increment();
            return _hashCalculater.DeserializeValue(data);
        }

        public async Task<InnerData> ReadAsync(object key)
        {
            var hash = _hashCalculater.CalculateHashFromKey(key);
            var transaction = _distributor.CreateTransaction(hash);

            transaction.OperationName = OperationName.Read;
            transaction.OperationType = OperationType.Sync;

            if (!transaction.IsError)
            {
                SomeAsyncWorkForRead(transaction);
                CompleteTransactionKeyOnly(key, transaction);
            }
            else
            {
                var task = new TaskCompletionSource<InnerData>();
                task.SetResult(new InnerData(transaction));

                PerfCounters.ProxyCounters.Instance.ReadCount.Increment();
                return await task.Task;
            }
            
            var res =  await transaction.InnerSupportCallback.Task;
            PerfCounters.ProxyCounters.Instance.ReadCount.Increment();
            return res;
        }

        public UserTransaction CustomOperation(object key, object value, string description)
        {
            var hash = _hashCalculater.CalculateHashFromKey(key);
            var transaction = _distributor.CreateTransaction(hash);

            transaction.OperationName = OperationName.Custom;
            transaction.OperationType = OperationType.Async;

            transaction.CustomOperationField = description;

            if (!transaction.IsError)
            {
                CompleteTransaction(key, value, transaction, false);
                PerfCounters.ProxyCounters.Instance.CustomOperationCount.Increment();
            }

            return transaction.UserTransaction;
        }

        public async Task<UserTransaction> CustomOperationSync(object key, object value, string description)
        {
            var hash = _hashCalculater.CalculateHashFromKey(key);
            var transaction = _distributor.CreateTransaction(hash);

            transaction.OperationName = OperationName.Custom;
            transaction.OperationType = OperationType.Sync;
            
            transaction.CustomOperationField = description;

            if (!transaction.IsError)
            {
                SomeAsyncWork(transaction);
                CompleteTransaction(key, value, transaction, false);
            }
            else
            {
                var task = new TaskCompletionSource<UserTransaction>();
                task.SetResult(transaction.UserTransaction);

                PerfCounters.ProxyCounters.Instance.CustomOperationCount.Increment();
                return  await task.Task;
            }

            var res = await transaction.UserSupportCallback.Task;
            PerfCounters.ProxyCounters.Instance.CustomOperationCount.Increment();
            return res;
        }

        public UserTransaction GetTransactionState(UserTransaction transaction)
        {
            return _processTransaction.GetTransaction(transaction);
        }

        public RemoteResult SayIAmHere(ServerId server)
        {
            return _distributor.SayIAmHere(server);
        }

        #endregion

        #region Private

        private void SomeAsyncWork(Transaction transaction)
        {
            transaction.UserSupportCallback = new TaskCompletionSource<UserTransaction>();
            _asyncProxyCache.AddToCache(transaction.CacheKey, transaction);
        }

        private void SomeAsyncWorkForRead(Transaction transaction)
        {
            transaction.InnerSupportCallback = new TaskCompletionSource<InnerData>();
            _asyncProxyCache.AddToCache(transaction.CacheKey, transaction);
        }

        private void CompleteTransaction(object key, object value, Transaction transaction,
            bool useGenericSerilize = true)
        {
            var serializeValue = useGenericSerilize
                ? _hashCalculater.SerializeValue(value)
                : _hashCalculater.SerializeOther(value);
            
            var serializeKey = _hashCalculater.SerializeKey(key);

            var process = new InnerData(transaction) {Data = serializeValue, Key = serializeKey};

            PerfCounters.ProxyCounters.Instance.CreateCount.Increment();
            process.Transaction.PerfTimer = PerfCounters.ProxyCounters.Instance.AverageTimer.StartNew();

            _processTransaction.ProcessData(process, _tableName);

            PerfCounters.ProxyCounters.Instance.IncomePerSec.OperationFinished();
        }

        private void CompleteTransactionKeyOnly(object key, Transaction transaction)
        {
            var serializeKey = _hashCalculater.SerializeKey(key);
            
            transaction.HashFromValue = _hashFromValue;
            var process = new InnerData(transaction) { Key = serializeKey };

            PerfCounters.ProxyCounters.Instance.CreateCount.Increment();
            process.Transaction.PerfTimer = PerfCounters.ProxyCounters.Instance.AverageTimer.StartNew();

            _processTransaction.ProcessData(process, _tableName);

            PerfCounters.ProxyCounters.Instance.IncomePerSec.OperationFinished();
        }
        
        #endregion
    }
}

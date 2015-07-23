using System;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Common.Timestamps;
using Qoollo.Impl.Modules.Cache;

namespace Qoollo.Impl.Proxy.Caches
{
    internal class AsyncProxyCache:CacheModule<Transaction>
    {
        public AsyncProxyCache(TimeSpan timeout) : base(timeout)
        {
        }

        protected override void RemovedCallback(string key, Transaction obj)
        {
            if (obj.UserSupportCallback != null)
            {
                SetError(obj);
                obj.UserSupportCallback.SetResult(obj.UserTransaction);                
            }

            if (obj.InnerSupportCallback != null)
            {
                SetError(obj);
                obj.InnerSupportCallback.SetResult(null);
            }
        }

        private void SetError(Transaction transaction)
        {
            transaction.SetError();
            transaction.AddErrorDescription(Errors.SyncOperationTimeout);

            transaction.MakeStampWithTransactionError("proxy AsyncCache");
        }
    }
}

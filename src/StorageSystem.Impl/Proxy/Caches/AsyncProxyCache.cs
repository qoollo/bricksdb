using System;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.Support;
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
                obj.SetError();
                obj.AddErrorDescription(Errors.SyncOperationTimeout);
                obj.UserSupportCallback.SetResult(obj.UserTransaction);
                //obj.UserSupportCallback.SetException(new TimeoutException(Errors.OperationTimeoutException));
            }

            if (obj.InnerSupportCallback != null)
            {
                obj.SetError();
                obj.AddErrorDescription(Errors.SyncOperationTimeout);
                obj.InnerSupportCallback.SetResult(null);
                //obj.InnerSupportCallback.SetException(new TimeoutException(Errors.OperationTimeoutException));
            }
        }
    }
}

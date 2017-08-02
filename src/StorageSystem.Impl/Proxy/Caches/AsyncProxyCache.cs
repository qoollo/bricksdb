using System;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Cache;
using Qoollo.Impl.Proxy.Interfaces;

namespace Qoollo.Impl.Proxy.Caches
{
    internal class AsyncProxyCache:CacheModule<Transaction>, IAsyncProxyCache
    {
        public AsyncProxyCache(ProxyCacheConfiguration proxyConfigurationCache) 
            : base(TimeSpan.FromMilliseconds(proxyConfigurationCache.Transaction))
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

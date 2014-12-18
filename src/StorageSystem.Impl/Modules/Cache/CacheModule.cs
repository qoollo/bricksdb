using System;
using System.Diagnostics.Contracts;
using System.Runtime.Caching;

namespace Qoollo.Impl.Modules.Cache
{
    internal abstract class CacheModule<T>:ControlModule where T :class 
    {
        private MemoryCache _cache;
        private TimeSpan _timeout;

        protected CacheModule(TimeSpan timeout)
        {
            Contract.Requires(timeout!=null);
            _timeout = timeout;
            _cache = new MemoryCache("CacheModule" + DateTime.Now);
        }

        public void AddToCache(string key, T obj)
        {
            var policy = new CacheItemPolicy()
                {
                    RemovedCallback = RemovedCallbackInner,
                    AbsoluteExpiration = DateTime.Now.Add(_timeout)
                };
            _cache.Add(key, obj, policy);
        }

        private void RemovedCallbackInner(CacheEntryRemovedArguments arguments)
        {
            if(arguments.RemovedReason!= CacheEntryRemovedReason.Removed)
                RemovedCallback(arguments.CacheItem.Key, arguments.CacheItem.Value as T);
        }

        protected abstract void RemovedCallback(string key, T obj);

        protected void AddAliveToCache(string key, T obj, TimeSpan timeout)
        {
            _cache.Add(key, obj, DateTime.Now.Add(timeout));
        }

        public T Get(string key)
        {
            var ret = _cache.Get(key);
            if (ret == null)
                return null;
            return ret as T;
        }

        public void Remove(string key)
        {
            _cache.Remove(key);
        }

        public void Update(string key, T obj, TimeSpan timeout)
        {
            Remove(key);
            AddAliveToCache(key, obj, timeout);
        }

        protected override void Dispose(bool isUserCall)
        {
            base.Dispose(isUserCall);
            if(isUserCall)
                _cache.Dispose();
        }
    }
}

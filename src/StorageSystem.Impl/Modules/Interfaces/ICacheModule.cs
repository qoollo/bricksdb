using System;

namespace Qoollo.Impl.Modules.Interfaces
{
    internal interface ICacheModule<T> where T : class
    {
        void AddToCache(string key, T obj);
        T Get(string key);
        void Remove(string key);
        void Update(string key, T obj, TimeSpan timeout);
    }
}
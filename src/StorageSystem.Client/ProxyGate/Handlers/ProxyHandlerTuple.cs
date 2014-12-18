using System;
using System.Threading.Tasks;
using Qoollo.Client.Request;

namespace Qoollo.Client.ProxyGate.Handlers
{
    internal class ProxyHandlerTuple<TKey, TValue> : ProxyHandlerBase, IStorage<TKey, TValue>
    {
        private readonly Func<bool> _isEmpty;

        public ProxyHandlerTuple(IStorage<TKey, TValue> emptyHandler, IStorage<TKey, TValue> handler, Func<bool> isEmpty)
        {
            _isEmpty = isEmpty;
            Handler = handler;
            EmptyHandler = emptyHandler;
        }

        public IStorage<TKey, TValue> Handler { get; private set; }
        public IStorage<TKey, TValue> EmptyHandler { get; private set; }

        private TRet InnerFunc<TRet>(Func<IStorage<TKey, TValue>,TRet> func )
        {
            if (_isEmpty())
                return func(EmptyHandler);
            return func(Handler);
        }

        public RequestDescription Create(TKey key, TValue value)
        {
            return InnerFunc(handler => handler.Create(key, value));
        }

        public RequestDescription Update(TKey key, TValue value)
        {
            return InnerFunc(handler => handler.Update(key, value));
        }

        public RequestDescription Delete(TKey key)
        {
            return InnerFunc(handler => handler.Delete(key));
        }

        public RequestDescription CreateSync(TKey key, TValue value)
        {
            return InnerFunc(handler => handler.CreateSync(key, value));
        }

        public RequestDescription UpdateSync(TKey key, TValue value)
        {
            return InnerFunc(handler => handler.UpdateSync(key, value));
        }

        public RequestDescription DeleteSync(TKey key)
        {
            return InnerFunc(handler => handler.DeleteSync(key));
        }

        public Task<RequestDescription> CreateAsync(TKey key, TValue value)
        {
            return InnerFunc(handler => handler.CreateAsync(key, value));
        }

        public Task<RequestDescription> UpdateAsync(TKey key, TValue value)
        {
            return InnerFunc(handler => handler.UpdateAsync(key, value));
        }

        public Task<RequestDescription> DeleteAsync(TKey key)
        {
            return InnerFunc(handler => handler.DeleteAsync(key));
        }

        public TValue Read(TKey key, out RequestDescription result)
        {
            RequestDescription r = null;
            var ret =  InnerFunc(handler => handler.Read(key, out r));
            result = r;
            return ret;
        }

        public Task<AsyncReadResult<TValue>> ReadAsync(TKey key)
        {
            return InnerFunc(handler => handler.ReadAsync(key));
        }

        public RequestDescription CustomOperation(TKey key, object value, string description)
        {
            return InnerFunc(handler => handler.CustomOperation(key, value, description));
        }

        public RequestDescription CustomOperationSync(TKey key, object value, string description)
        {
            return InnerFunc(handler => handler.CustomOperationSync(key, value, description));
        }

        public Task<RequestDescription> CustomOperationAsync(TKey key, object value, string description)
        {
            return InnerFunc(handler => handler.CustomOperationAsync(key, value, description));
        }

        public RequestDescription GetOperationState(RequestDescription description)
        {
            return InnerFunc(handler => handler.GetOperationState(description));
        }

        public RequestDescription SayIAmHere(string host, int port)
        {
            return InnerFunc(handler => handler.SayIAmHere(host, port));
        }
    }
}

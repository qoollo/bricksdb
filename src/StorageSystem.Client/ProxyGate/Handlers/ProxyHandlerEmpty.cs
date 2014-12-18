using System.Threading.Tasks;
using Qoollo.Client.Request;

namespace Qoollo.Client.ProxyGate.Handlers
{
    internal class ProxyHandlerEmpty<TKey, TValue> : ProxyHandlerBase, IStorage<TKey, TValue>
    {        
        public RequestDescription Create(TKey key, TValue value)
        {
            return new RequestDescription();
        }

        public RequestDescription Update(TKey key, TValue value)
        {
            return new RequestDescription();
        }

        public RequestDescription Delete(TKey key)
        {
            return new RequestDescription();
        }

        public RequestDescription CreateSync(TKey key, TValue value)
        {
            return new RequestDescription();
        }

        public RequestDescription UpdateSync(TKey key, TValue value)
        {
            return new RequestDescription();
        }

        public RequestDescription DeleteSync(TKey key)
        {
            return new RequestDescription();
        }

        public async Task<RequestDescription> CreateAsync(TKey key, TValue value)
        {
            return new RequestDescription();
        }

        public async Task<RequestDescription> UpdateAsync(TKey key, TValue value)
        {
            return new RequestDescription();
        }

        public async Task<RequestDescription> DeleteAsync(TKey key)
        {
            return new RequestDescription();
        }

        public TValue Read(TKey key, out RequestDescription result)
        {
            result = new RequestDescription();

            return default(TValue);
        }

        public async Task<AsyncReadResult<TValue>> ReadAsync(TKey key)
        {
            return new AsyncReadResult<TValue>(new RequestDescription(), default(TValue));  
        }

        public RequestDescription CustomOperation(TKey key, object value, string description)
        {
            return new RequestDescription();
        }

        public RequestDescription CustomOperationSync(TKey key, object value, string description)
        {
            return new RequestDescription();
        }

        public async Task<RequestDescription> CustomOperationAsync(TKey key, object value, string description)
        {
            return new RequestDescription();
        }

        public RequestDescription GetOperationState(RequestDescription description)
        {
            return new RequestDescription();
        }

        public RequestDescription SayIAmHere(string host, int port)
        {
            return new RequestDescription();
        }
    }
}

using System;
using System.Threading.Tasks;
using Qoollo.Client.Request;
using Qoollo.Client.Support;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Client.ProxyGate.Handlers
{
    internal class ProxyHandler<TKey, TValue> : ProxyHandlerBase, IStorage<TKey, TValue>
    {
        private IStorageInner _api;
        private IDataProvider<TKey, TValue> _dataProvider;

        public ProxyHandler(IStorageInner api, IDataProvider<TKey, TValue> dataProvider)
        {
            _api = api;
            _dataProvider = dataProvider;
        }

        public RequestDescription Create(TKey key, TValue value)
        {
            var utr = _api.Create(key, value);
            return new RequestDescription(utr);
        }

        public RequestDescription Update(TKey key, TValue value)
        {
            var utr = _api.Update(key, value);
            return new RequestDescription(utr);
        }

        public RequestDescription Delete(TKey key)
        {
            var utr = _api.Delete(key);
            return new RequestDescription(utr);
        }

        public RequestDescription CreateSync(TKey key, TValue value)
        {
            var utr = _api.CreateSync(key, value);

            RequestDescription ret = null;
            utr.Wait();

            ret = new RequestDescription(utr.Result);

            return ret;
        }

        public RequestDescription UpdateSync(TKey key, TValue value)
        {
            var utr = _api.UpdateSync(key, value);

            RequestDescription ret = null;

            utr.Wait();
            ret = new RequestDescription(utr.Result);
            return ret;
        }

        public RequestDescription DeleteSync(TKey key)
        {
            var utr = _api.DeleteSync(key);

            RequestDescription ret = null;


            utr.Wait();
            ret = new RequestDescription(utr.Result);

            return ret;
        }

        public async Task<RequestDescription> CreateAsync(TKey key, TValue value)
        {
            var utr = await _api.CreateSync(key, value);
            return new RequestDescription(utr);
        }

        public async Task<RequestDescription> UpdateAsync(TKey key, TValue value)
        {
            var utr = await _api.UpdateSync(key, value);
            return new RequestDescription(utr);
        }

        public async Task<RequestDescription> DeleteAsync(TKey key)
        {
            var utr = await _api.DeleteSync(key);
            return new RequestDescription(utr);
        }

        public TValue Read(TKey key, out RequestDescription result)
        {
            UserTransaction utr = null;

            var ret = _api.Read(key, out utr);

            result = new RequestDescription(utr);

            if (ret == null)
            {
                result.DataNotFound();
                return default(TValue);
            }

            return (TValue) ret;
        }

        public async Task<AsyncReadResult<TValue>> ReadAsync(TKey key)
        {
            var innerData = await _api.ReadAsync(key);

            TValue data = default(TValue);

            if (innerData == null || innerData.Transaction == null || innerData.Transaction.UserTransaction == null)
            {
                return new AsyncReadResult<TValue>(new RequestDescription(Errors.FailRead), data);
            }

            var result = new RequestDescription(innerData.Transaction.UserTransaction);            

            if(innerData.Data!=null)
                data = _dataProvider.DeserializeValue(innerData.Data);
            else
                result.DataNotFound();

            return new AsyncReadResult<TValue>(result, data);
        }

        public RequestDescription CustomOperation(TKey key, object value, string description)
        {
            var utr = _api.CustomOperation(key, value, description);
            return new RequestDescription(utr);
        }

        public RequestDescription CustomOperationSync(TKey key, object value, string description)
        {
            var utr = _api.CustomOperationSync(key, value, description);

            RequestDescription ret = null;

            utr.Wait();
            ret = new RequestDescription(utr.Result);

            return ret;
        }

        public async Task<RequestDescription> CustomOperationAsync(TKey key, object value, string description)
        {
            var utr = await _api.CustomOperationSync(key, value, description);
            return new RequestDescription(utr);
        }

        public RequestDescription GetOperationState(RequestDescription description)
        {
            var utr = new UserTransaction(description.DistributorHash);
            utr.SetCacheKey(description.CacheKey);

            utr = _api.GetTransactionState(utr);

            if (utr == null)
                return null;

            return new RequestDescription(utr);
        }

        public RequestDescription SayIAmHere(string host, int port)
        {
            var result = _api.SayIAmHere(new ServerId(host, port));
            return new RequestDescription(result);
        }
    }
}

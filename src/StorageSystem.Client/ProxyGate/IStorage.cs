using System.Threading.Tasks;
using Qoollo.Client.Request;

namespace Qoollo.Client.ProxyGate
{
    public interface IStorage<TKey, TValue>
    {
        RequestDescription Create(TKey key, TValue value);
        RequestDescription Update(TKey key, TValue value);
        RequestDescription Delete(TKey key);

        RequestDescription CreateSync(TKey key, TValue value);
        RequestDescription UpdateSync(TKey key, TValue value);
        RequestDescription DeleteSync(TKey key);

        Task<RequestDescription> CreateAsync(TKey key, TValue value);
        Task<RequestDescription> UpdateAsync(TKey key, TValue value);
        Task<RequestDescription> DeleteAsync(TKey key);

        TValue Read(TKey key, out RequestDescription result);
        Task<AsyncReadResult<TValue>> ReadAsync(TKey key);

        RequestDescription CustomOperation(TKey key, object value, string description);
        RequestDescription CustomOperationSync(TKey key, object value, string description);
        Task<RequestDescription> CustomOperationAsync(TKey key, object value, string description);

        RequestDescription GetOperationState(RequestDescription description);

        RequestDescription SayIAmHere(string host, int port);
    }
}

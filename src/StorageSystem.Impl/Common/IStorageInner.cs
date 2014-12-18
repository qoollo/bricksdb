using System.Threading.Tasks;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Common
{
    internal interface IStorageInner
    {
        UserTransaction Create(object key, object value);
        UserTransaction Update(object key, object value);
        UserTransaction Delete(object key);

        Task<UserTransaction> CreateSync(object key, object value);
        Task<UserTransaction> UpdateSync(object key, object value);
        Task<UserTransaction> DeleteSync(object key);

        object Read(object key, out UserTransaction result);
        Task<InnerData> ReadAsync(object key);

        UserTransaction CustomOperation(object key, object value, string description);
        Task<UserTransaction> CustomOperationSync(object key, object value, string description);

        UserTransaction GetTransactionState(UserTransaction transaction);

        RemoteResult SayIAmHere(ServerId server);
    }
}

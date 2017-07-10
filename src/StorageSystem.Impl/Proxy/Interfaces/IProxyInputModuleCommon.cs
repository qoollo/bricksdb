using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.HashHelp;

namespace Qoollo.Impl.Proxy.Interfaces
{
    internal interface IProxyInputModuleCommon
    {
        IStorageInner CreateApi(string tableName, bool hashFromValue, IHashCalculater hashCalculater);
        UserTransaction GetTransaction(UserTransaction transaction);
        void ProcessData(InnerData ev, string tableName);
    }
}
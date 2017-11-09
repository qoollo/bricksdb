using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;

namespace Qoollo.Impl.Proxy.Interfaces
{
    internal interface IProxyMainLogicModule
    {
        UserTransaction GetTransaction(UserTransaction transaction);
        bool Process(InnerData ev);
    }
}
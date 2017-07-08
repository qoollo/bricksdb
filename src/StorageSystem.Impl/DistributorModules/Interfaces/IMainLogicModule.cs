using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.DistributorModules.Transaction;

namespace Qoollo.Impl.DistributorModules.Interfaces
{
    internal interface IMainLogicModule
    {
        UserTransaction GetTransactionState(UserTransaction transaction);
        void ProcessWithData(InnerData data, TransactionExecutor executor);
    }
}
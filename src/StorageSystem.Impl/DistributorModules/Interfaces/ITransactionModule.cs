using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.DistributorModules.Transaction;
using Qoollo.Turbo.ObjectPools;

namespace Qoollo.Impl.DistributorModules.Interfaces
{
    internal interface ITransactionModule
    {
        void ProcessSyncTransaction(InnerData data);
        void ProcessWithExecutor(InnerData data, TransactionExecutor executor);
        RentedElementMonitor<TransactionExecutor> Rent();
        void TransactionAnswerIncome(Common.Data.TransactionTypes.Transaction transaction);
    }
}
using System.Diagnostics.Contracts;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.DistributorModules.Transaction;
using Qoollo.Impl.Modules.ParallelWork;
using Qoollo.Turbo.ObjectPools;

namespace Qoollo.Impl.DistributorModules.ParallelWork
{
    internal class OneThreadProcess:SingleParallelWorkBase<InnerData>
    {
        private readonly MainLogicModule _main;
        private RentedElementMonitor<TransactionExecutor> _transaction;

        public OneThreadProcess(MainLogicModule main, TransactionModule transactionModule)
        {
            Contract.Requires(main != null);
            Contract.Requires(transactionModule != null);

            _main = main;
            _transaction = transactionModule.Rent();
        }

        public override void Process(InnerData data)
        {
            PerfCounters.DistributorCounters.Instance.TransactionCount.Increment();
            _main.ProcessWithData(data, _transaction.Element);
        }

        protected override void Dispose(bool isUserCall)
        {            
            base.Dispose(isUserCall);
            _transaction.Dispose();
        }
    }
}

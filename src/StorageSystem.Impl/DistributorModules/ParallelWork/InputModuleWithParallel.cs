using System.Diagnostics.Contracts;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Timestamps;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DistributorModules.Transaction;
using Qoollo.Impl.Modules.ParallelWork;

namespace Qoollo.Impl.DistributorModules.ParallelWork
{
    internal class InputModuleWithParallel : ParallelWorkModule <InnerData>,IInputModule
    {
        private readonly MainLogicModule _main;
        private readonly TransactionModule _transactionModule;

        public InputModuleWithParallel(QueueConfiguration configuration, MainLogicModule main,
            TransactionModule transactionModule)
            : base(configuration)
        {
            Contract.Requires(main != null);
            Contract.Requires(transactionModule != null);
            _main = main;
            _transactionModule = transactionModule;
        }

        protected override bool CreateWorker(out SingleParallelWorkBase<InnerData> worker)
        {
            worker = new OneThreadProcess(_main, _transactionModule);
            return true;
        }

        public void ProcessAsync(InnerData data)
        {
            data.Transaction.PerfTimer = PerfCounters.DistributorCounters.Instance.AverageTimer.StartNew();
            PerfCounters.DistributorCounters.Instance.IncomePerSec.OperationFinished();
            data.Transaction.Start("distributor");       
    
            Add(data);
        }
    }
}

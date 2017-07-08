using Ninject;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.DistributorModules.Interfaces;
using Qoollo.Impl.DistributorModules.Transaction;
using Qoollo.Impl.Modules.ParallelWork;
using Qoollo.Turbo.ObjectPools;

namespace Qoollo.Impl.DistributorModules.ParallelWork
{
    internal class OneThreadProcess:SingleParallelWorkBase<InnerData>
    {
        private IMainLogicModule _main;
        private RentedElementMonitor<TransactionExecutor> _transaction;

        public OneThreadProcess(StandardKernel kernel)
            :base(kernel)
        {

        }

        public override void Start()
        {
            _main = Kernel.Get<IMainLogicModule>();
            var transaction = Kernel.Get<ITransactionModule>();
            _transaction = transaction.Rent();

            base.Start();
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

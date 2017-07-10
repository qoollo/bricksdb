using Ninject;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DistributorModules.Interfaces;
using Qoollo.Impl.Modules.ParallelWork;

namespace Qoollo.Impl.DistributorModules.ParallelWork
{
    internal class InputModuleWithParallel : ParallelWorkModule <InnerData>,IInputModule
    {
        public InputModuleWithParallel(StandardKernel kernel, QueueConfiguration configuration)
            : base(kernel, configuration)
        {
        }

        protected override bool CreateWorker(out SingleParallelWorkBase<InnerData> worker)
        {
            worker = new OneThreadProcess(Kernel);
            return true;
        }

        public void ProcessAsync(InnerData ev)
        {
            ev.Transaction.PerfTimer = PerfCounters.DistributorCounters.Instance.AverageTimerWithQueue.StartNew();
            PerfCounters.DistributorCounters.Instance.IncomePerSec.OperationFinished();

            Add(ev);
        }
    }
}

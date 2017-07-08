using System.Diagnostics.Contracts;
using System.Threading;
using Ninject;
using Qoollo.Impl.DistributorModules.Interfaces;
using Qoollo.Turbo.ObjectPools;

namespace Qoollo.Impl.DistributorModules.Transaction
{
    internal class TransactionPool : DynamicPoolManager<TransactionExecutor>
    {
        private readonly StandardKernel _kernel;
        private readonly int _countReplics;

        public TransactionPool(StandardKernel kernel, int maxElemCount, int countReplics)
            : base(maxElemCount)
        {
            Contract.Requires(countReplics>0);
            _kernel = kernel;
            _countReplics = countReplics;
        }

        public void Start()
        {
        }

        protected override bool CreateElement(out TransactionExecutor elem, int timeout, CancellationToken token)
        {
            var net = _kernel.Get<IDistributorNetModule>();
            elem = new TransactionExecutor(net, _countReplics, _kernel);
            return true;
        }

        protected override bool IsValidElement(TransactionExecutor elem)
        {
            return true;
        }

        protected override void DestroyElement(TransactionExecutor elem)
        {
        }
    }
}

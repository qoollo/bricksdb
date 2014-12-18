using System.Diagnostics.Contracts;
using System.Threading;
using Core.ServiceClasses.Pool;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DistributorModules.DistributorNet.Interfaces;

namespace Qoollo.Impl.DistributorModules.Transaction
{
    internal class TransactionPool : UnifiedDynamicSizePoolManager<TransactionExecutor>
    {
        private INetModule _net;
        private int _countReplics;

        public TransactionPool(int maxElemCount, INetModule net, DistributorHashConfiguration configuration)
            : base(maxElemCount)
        {
            Contract.Requires(_net!=null);
            Contract.Requires(configuration!=null);
            _net = net;
            _countReplics = configuration.CountReplics;
        }

        protected override bool CreateElement(out TransactionExecutor elem, int timeout, CancellationToken token)
        {
            elem = new TransactionExecutor(_net, _countReplics);
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

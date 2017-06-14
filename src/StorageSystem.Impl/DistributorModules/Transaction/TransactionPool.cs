using System.Diagnostics.Contracts;
using System.Threading;
using Qoollo.Impl.DistributorModules.DistributorNet.Interfaces;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Turbo.ObjectPools;

namespace Qoollo.Impl.DistributorModules.Transaction
{
    internal class TransactionPool : DynamicPoolManager<TransactionExecutor>
    {
        private readonly INetModule _net;
        private readonly int _countReplics;
        private readonly GlobalQueueInner _queue;

        public TransactionPool(int maxElemCount, INetModule net, int countReplics)
            : base(maxElemCount)
        {
            Contract.Requires(_net!=null);
            Contract.Requires(countReplics>0);
            _net = net;
            _countReplics = countReplics;
            _queue = GlobalQueue.Queue;
        }

        protected override bool CreateElement(out TransactionExecutor elem, int timeout, CancellationToken token)
        {
            elem = new TransactionExecutor(_net, _countReplics, _queue);
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

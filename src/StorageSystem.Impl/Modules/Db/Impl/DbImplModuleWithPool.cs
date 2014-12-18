using System.Diagnostics.Contracts;
using System.Threading;
using Core.ServiceClasses.Pool;
using Qoollo.Impl.Modules.Pools;

namespace Qoollo.Impl.Modules.Db.Impl
{
    internal abstract class DbImplModuleWithPool<TConnection, TConnectionParam, TCommand, TReader> : DbImplModule<TCommand, TConnection, TReader>
        where TConnection : class
        where TConnectionParam : class
    {
        private CommonPool<TConnection> _pool;
        private TConnectionParam _connectionParam;

        protected DbImplModuleWithPool(TConnectionParam connectionParam, int maxCountElementInPool, int trimPeriod)
        {
            Contract.Requires(connectionParam != null);            

            _connectionParam = connectionParam;
            _pool = new CommonPool<TConnection>(CreateElementInner, IsValidElement, DestroyElement, maxCountElementInPool,
                trimPeriod,
                "DbPool");
        }

        public override void Start()
        {
            _pool.FillPoolUpTo(_pool.MaxElementCount);
        }

        protected UnifiedPoolElement<TConnection> RentConnection()
        {
            return _pool.Rent();
        }

        protected bool CreateElementInner(out TConnection elem, int timeout, CancellationToken token)
        {
            return CreateElement(out elem, _connectionParam, timeout, token);
        }

        protected abstract bool CreateElement(out TConnection elem, TConnectionParam connectionParam, int timeout,
            CancellationToken token);

        protected abstract bool IsValidElement(TConnection elem);

        protected abstract void DestroyElement(TConnection elem);

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                _pool.Dispose();
            }
            base.Dispose(isUserCall);
        }
    }
}

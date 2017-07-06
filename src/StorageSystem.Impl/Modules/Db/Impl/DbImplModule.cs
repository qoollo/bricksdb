using Ninject;
using Qoollo.Impl.Common;
using Qoollo.Turbo.ObjectPools;

namespace Qoollo.Impl.Modules.Db.Impl
{
    public abstract class DbImplModule<TCommand, TConnection, TReader> : ControlModule where TConnection : class
    {
        protected DbImplModule(StandardKernel kernel) : base(kernel)
        {
        }

        public abstract RemoteResult ExecuteNonQuery(TCommand command);

        public abstract DbReader<TReader> CreateReader(TCommand command);

        public abstract RentedElementMonitor<TConnection> RentConnectionInner();
    }
}

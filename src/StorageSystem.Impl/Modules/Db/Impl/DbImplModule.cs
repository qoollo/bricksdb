using Core.ServiceClasses.Pool;
using Qoollo.Impl.Common;

namespace Qoollo.Impl.Modules.Db.Impl
{
    internal abstract class DbImplModule<TCommand, TConnection, TReader> : ControlModule where TConnection : class
    {
        public abstract RemoteResult ExecuteNonQuery(TCommand command);

        public abstract DbReader<TReader> CreateReader(TCommand command);

        public abstract UnifiedPoolElement<TConnection> RentConnectionInner();
    }
}

using System;
using System.Threading.Tasks;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Modules.Net.ConnectionBehavior
{
    internal abstract class ConnectionBehaviorBase<TConnection> : IConnectionBehavior<TConnection>
    {
        public ServerId Server { get; private set; }

        protected ConnectionBehaviorBase(ServerId server)
        {
            Server = server;
        }

        public abstract bool Connect();

        public abstract TResult SendFunc<TResult, TApi>(Func<TApi, TResult> func, 
            Func<Exception, TResult> errorRet,
            string errorLogFromData) where TApi : class;

        public abstract Task<TResult> SendAsyncFunc<TResult, TApi>(Func<TApi, Task<TResult>> func,
            Func<Exception, TResult> errorRet,
            string errorLogFromData) where TApi : class;

        protected abstract void Dispose(bool isUserCall);

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
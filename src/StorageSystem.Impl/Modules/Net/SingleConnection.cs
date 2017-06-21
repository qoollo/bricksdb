using System;
using System.Threading.Tasks;
using Ninject;
using Ninject.Parameters;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Net.ConnectionBehavior;
using Qoollo.Impl.TestSupport;

namespace Qoollo.Impl.Modules.Net
{
    internal abstract class SingleConnection<T> : ControlModule
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();
        private readonly IConnectionBehavior<T> _connection;

        public ServerId Server => _connection.Server;

        protected SingleConnection(ServerId server, ConnectionConfiguration configuration,
            ConnectionTimeoutConfiguration timeoutConfiguration)
        {
            _connection = InitInjection.Kernel.Get<IConnectionBehavior<T>>(
                new ConstructorArgument("server", server),
                new ConstructorArgument("configuration", configuration),
                new ConstructorArgument("timeoutConfiguration", timeoutConfiguration));
        }

        public bool Connect()
        {
            return _connection.Connect();
        }

        protected TResult SendFunc<TResult, TApi>(Func<TApi, TResult> func, Func<Exception, TResult> errorRet,
            string errorLogFromData) where TApi : class
        {
            return _connection.SendFunc(func, errorRet, errorLogFromData);
        }

        protected Task<TResult> SendAsyncFunc<TResult, TApi>(Func<TApi, Task<TResult>> func,
            Func<Exception, TResult> errorRet,
            string errorLogFromData) where TApi : class
        {
            return _connection.SendAsyncFunc(func, errorRet, errorLogFromData);
        }

        protected override void Dispose(bool isUserCall)
        {
            _logger.DebugFormat("Dispose connection to remote server: {0}", Server);
            _connection.Dispose();
            base.Dispose(isUserCall);
        }
    }
}

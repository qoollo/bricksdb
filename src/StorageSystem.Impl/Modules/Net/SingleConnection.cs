using System;
using System.Threading.Tasks;
using Ninject;
using Ninject.Parameters;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Configurations.Queue;
using Qoollo.Impl.Modules.Net.ConnectionBehavior;

namespace Qoollo.Impl.Modules.Net
{
    internal abstract class SingleConnection<T> : ControlModule
    {
        private readonly Qoollo.Logger.Logger _logger = Logger.Logger.Instance.GetThisClassLogger();

        private IConnectionBehavior<T> _connection;
        public ServerId Server { get; }

        protected SingleConnection(StandardKernel kernel, ServerId server,
            ConnectionTimeoutConfiguration timeoutConfiguration)
            :base(kernel)
        {
            Server = server;
        }

        public bool Connect()
        {
            var config = Kernel.Get<ICommonConfiguration>();
            _connection = Kernel.Get<IConnectionBehavior<T>>(
                new ConstructorArgument("server", Server),
                new ConstructorArgument("configuration", config.Connection),
                new ConstructorArgument("timeoutConfiguration", 1));

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

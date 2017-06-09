using System;
using System.Reflection;
using System.Threading.Tasks;
using Ninject;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Modules.Net.ConnectionBehavior;

namespace Qoollo.Tests.NetMock
{
    internal class MockConnection<TConnection>: ConnectionBehaviorBase<TConnection>
    {
        private readonly INetMock _netMock;
        private TConnection _server;

        public MockConnection(ServerId server) : base(server)
        {
            var kernel = new StandardKernel();
            kernel.Load(Assembly.GetExecutingAssembly());

            _netMock = kernel.Get<INetMock>();
        }

        public override bool Connect()
        {
            return _netMock.TryConnectClient(Server, out _server);
        }

        public override Task<TResult> SendAsyncFunc<TResult, TApi>(Func<TApi, Task<TResult>> func, Func<Exception, TResult> errorRet, string errorLogFromData)
        {
            return func(_server as TApi);
        }        

        public override TResult SendFunc<TResult, TApi>(Func<TApi, TResult> func, Func<Exception, TResult> errorRet, string errorLogFromData)
        {
            return func(_server as TApi);
        }

        protected override void Dispose(bool isUserCall)
        {
            throw new NotImplementedException();
        }
    }
}
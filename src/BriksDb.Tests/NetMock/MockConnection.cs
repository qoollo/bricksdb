using System;
using System.Threading.Tasks;
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
            _netMock = NetMock.Instance;
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
            
        }
    }
}
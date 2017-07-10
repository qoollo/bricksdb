using System.Reflection;
using Ninject;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Net.ReceiveBehavior;

namespace Qoollo.Tests.NetMock
{
    internal class MockReceive<TReceive>: ReceiveBehaviorBase<TReceive>
    {
        private readonly NetReceiverConfiguration _configuration;
        public readonly TReceive Server;
        private readonly INetMock _netMock;

        public MockReceive(NetReceiverConfiguration configuration, TReceive server) 
            : base(configuration, server)
        {
            _configuration = configuration;
            Server = server;

            _netMock = NetMock.Instance;
        }

        public override void Start()
        {
            _netMock.AddServer(new ServerId(_configuration.Host, _configuration.Port), this);
        }

        protected override void Dispose(bool isUserCall)
        {
            _netMock.RemoveServer<TReceive>(new ServerId(_configuration.Host, _configuration.Port));
        }
    }
}
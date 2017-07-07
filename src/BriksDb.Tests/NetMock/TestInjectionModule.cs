using Qoollo.Impl.Modules.Net.ConnectionBehavior;
using Qoollo.Impl.Modules.Net.ReceiveBehavior;
using Qoollo.Impl.TestSupport;

namespace Qoollo.Tests.NetMock
{
    public class TestInjectionModule:InjectionModule
    {
        public override void Load()
        {
            base.Load();

            Bind<INetMock>().To<NetMock>().InSingletonScope();
        }

        protected override void BindClientAndServer<TType>()
        {
            Bind<IConnectionBehavior<TType>>().To<MockConnection<TType>>();
            Bind<IReceiveBehavior<TType>>().To<MockReceive<TType>>();
        }
    }
}
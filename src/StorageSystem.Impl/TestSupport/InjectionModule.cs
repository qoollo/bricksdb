using Ninject.Modules;
using Qoollo.Impl.Modules.Net.ConnectionBehavior;
using Qoollo.Impl.Modules.Net.ReceiveBehavior;
using Qoollo.Impl.NetInterfaces.Distributor;
using Qoollo.Impl.NetInterfaces.Proxy;
using Qoollo.Impl.NetInterfaces.Writer;

namespace Qoollo.Impl.TestSupport
{
    public class InjectionModule:NinjectModule
    {
        public override void Load()
        {
            BindClientAndServer<ICommonNetReceiverWriterForCollector>();
            BindClientAndServer<ICommonNetReceiverWriterForWrite>();

            BindClientAndServer<ICommonNetReceiverForDb>();
            BindClientAndServer<ICommonNetReceiverForProxy>();

            BindClientAndServer<ICommonProxyNetReceiver>();
        }

        private void BindClientAndServer<TType>()
        {
            Bind<IConnectionBehavior<TType>>().To<WpfConnection<TType>>();
            Bind<IReceiveBehavior<TType>>().To<NetReceiveBehavior<TType>>();
        }
    }
}
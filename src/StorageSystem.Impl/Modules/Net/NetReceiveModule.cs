using System.Diagnostics.Contracts;
using System.Reflection;
using Ninject;
using Ninject.Parameters;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Net.ReceiveBehavior;

namespace Qoollo.Impl.Modules.Net
{
    internal abstract class NetReceiveModule<T> : ControlModule
    {
        private readonly IReceiveBehavior<T> _receive; 

        protected NetReceiveModule(NetReceiverConfiguration configuration)
        {
            Contract.Requires(configuration != null);

            var kernel = new StandardKernel();
            kernel.Load(Assembly.GetExecutingAssembly());

            _receive = kernel.Get<IReceiveBehavior<T>>(new ConstructorArgument("configuration", configuration));
        }

        public override void Start()
        {
            _receive.Start();
        }

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
                _receive.Dispose();

            base.Dispose(isUserCall);
        }
    }
}

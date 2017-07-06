using Ninject;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DistributorModules.ParallelWork;
using Qoollo.Impl.Modules;

namespace Qoollo.Impl.DistributorModules.DistributorNet
{
    internal class NetDistributorReceiver : ControlModule
    {
        private readonly NetDistributorReceiverForDb _distributorReceiverForDb;
        private readonly NetDistributorReceiverForProxy _distributorReceiverForProxy;

        public NetDistributorReceiver(StandardKernel kernel, MainLogicModule main, IInputModule input,
            DistributorModule distributorModule,
            NetReceiverConfiguration receiverConfigurationForDb,
            NetReceiverConfiguration receiverConfigurationForFroxy)
            : base(kernel)
        {
            _distributorReceiverForDb = new NetDistributorReceiverForDb(kernel, distributorModule, receiverConfigurationForDb);
            _distributorReceiverForProxy = new NetDistributorReceiverForProxy(kernel, main, input, distributorModule,
                receiverConfigurationForFroxy);
        }

        public override void Start()
        {
            _distributorReceiverForDb.Start();
            _distributorReceiverForProxy.Start();
        }

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                _distributorReceiverForDb.Dispose();
                _distributorReceiverForProxy.Dispose();
            }

            base.Dispose(isUserCall);
        }
    }
}

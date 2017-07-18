using Ninject;
using Qoollo.Impl.Configurations.Queue;
using Qoollo.Impl.Modules;

namespace Qoollo.Impl.DistributorModules.DistributorNet
{
    internal class NetDistributorReceiver : ControlModule
    {
        private NetDistributorReceiverForDb _distributorReceiverForDb;
        private NetDistributorReceiverForProxy _distributorReceiverForProxy;

        public NetDistributorReceiver(StandardKernel kernel): base(kernel)
        {
        }

        public override void Start()
        {
            var config = Kernel.Get<IDistributorConfiguration>();

            _distributorReceiverForDb = new NetDistributorReceiverForDb(Kernel, config.NetWriter);
            _distributorReceiverForProxy = new NetDistributorReceiverForProxy(Kernel, config.NetProxy);

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

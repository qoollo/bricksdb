using Qoollo.Impl.Configurations;
using Qoollo.Impl.DistributorModules.ParallelWork;
using Qoollo.Impl.Modules;

namespace Qoollo.Impl.DistributorModules.DistributorNet
{
    internal class NetDistributorReceiver : ControlModule
    {
        private NetDistributorReceiverForDb _distributorReceiverForDb;
        private NetDistributorReceiverForProxy _distributorReceiverForProxy;

        public NetDistributorReceiver(MainLogicModule main, IInputModule input, DistributorModule distributorModule,
                                      NetReceiverConfiguration receiverConfigurationForDb,
                                      NetReceiverConfiguration receiverConfigurationForFroxy)
        {
            _distributorReceiverForDb = new NetDistributorReceiverForDb(distributorModule, receiverConfigurationForDb);
            _distributorReceiverForProxy = new NetDistributorReceiverForProxy(main, input, distributorModule,
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

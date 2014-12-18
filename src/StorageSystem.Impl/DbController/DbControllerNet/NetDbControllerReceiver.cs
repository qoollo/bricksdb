using Qoollo.Impl.Configurations;
using Qoollo.Impl.DbController.Distributor;
using Qoollo.Impl.Modules;

namespace Qoollo.Impl.DbController.DbControllerNet
{
    internal class NetDbControllerReceiver:ControlModule
    {
        private NetDbControllerReceiverForWrite _controllerReceiverForWrite;
        private NetDbControllerReceiverForCollector _controllerReceiverForCollector;

        public NetDbControllerReceiver(InputModule inputModule, DistributorModule distributor,
            NetReceiverConfiguration receiverConfigurationForWrite,
            NetReceiverConfiguration receiverConfigurationForCollector)
        {
            _controllerReceiverForWrite = new NetDbControllerReceiverForWrite(inputModule, distributor,
                receiverConfigurationForWrite);
            _controllerReceiverForCollector = new NetDbControllerReceiverForCollector(inputModule, distributor,
                receiverConfigurationForCollector);
        }

        public override void Start()
        {
            _controllerReceiverForWrite.Start();
            _controllerReceiverForCollector.Start();
        }

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                _controllerReceiverForWrite.Dispose();
                _controllerReceiverForCollector.Dispose();
            }

            base.Dispose(isUserCall);
        }
    }
}

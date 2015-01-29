using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Writer.Distributor;

namespace Qoollo.Impl.Writer.WriterNet
{
    internal class NetWriterReceiver:ControlModule
    {
        private NetWriterReceiverForWrite _writerReceiverForWrite;
        private NetWriterReceiverForCollector _writerReceiverForCollector;

        public NetWriterReceiver(InputModule inputModule, DistributorModule distributor,
            NetReceiverConfiguration receiverConfigurationForWrite,
            NetReceiverConfiguration receiverConfigurationForCollector)
        {
            _writerReceiverForWrite = new NetWriterReceiverForWrite(inputModule, distributor,
                receiverConfigurationForWrite);
            _writerReceiverForCollector = new NetWriterReceiverForCollector(inputModule, distributor,
                receiverConfigurationForCollector);
        }

        public override void Start()
        {
            _writerReceiverForWrite.Start();
            _writerReceiverForCollector.Start();
        }

        protected override void Dispose(bool isUserCall)
        {
            if (isUserCall)
            {
                _writerReceiverForWrite.Dispose();
                _writerReceiverForCollector.Dispose();
            }

            base.Dispose(isUserCall);
        }
    }
}

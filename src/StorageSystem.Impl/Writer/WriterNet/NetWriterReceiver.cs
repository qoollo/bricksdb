using Ninject;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;

namespace Qoollo.Impl.Writer.WriterNet
{
    internal class NetWriterReceiver:ControlModule
    {
        private readonly NetWriterReceiverForWrite _writerReceiverForWrite;
        private readonly NetWriterReceiverForCollector _writerReceiverForCollector;

        public NetWriterReceiver(StandardKernel kernel, 
            NetReceiverConfiguration receiverConfigurationForWrite,
            NetReceiverConfiguration receiverConfigurationForCollector)
            :base(kernel)
        {
            _writerReceiverForWrite = new NetWriterReceiverForWrite(kernel, receiverConfigurationForWrite);
            _writerReceiverForCollector = new NetWriterReceiverForCollector(kernel, receiverConfigurationForCollector);
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

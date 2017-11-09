using Ninject;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;

namespace Qoollo.Impl.Writer.WriterNet
{
    internal class NetWriterReceiver:ControlModule
    {
        private NetWriterReceiverForWrite _writerReceiverForWrite;
        private NetWriterReceiverForCollector _writerReceiverForCollector;

        public NetWriterReceiver(StandardKernel kernel):base(kernel)
        {
        }

        public override void Start()
        {
            var config = Kernel.Get<IWriterConfiguration>();

            _writerReceiverForWrite = new NetWriterReceiverForWrite(Kernel, config.NetDistributor);
            _writerReceiverForCollector = new NetWriterReceiverForCollector(Kernel, config.NetCollector);

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

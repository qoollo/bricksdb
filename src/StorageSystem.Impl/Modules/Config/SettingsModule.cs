using Ninject;
using Qoollo.Impl.Configurations.Queue;

namespace Qoollo.Impl.Modules.Config
{
    internal class SettingsModule:ControlModule
    {
        private readonly string _connfigurationFilePath;

        public SettingsModule(StandardKernel kernel, string connfigurationFilePath) : base(kernel)
        {
            _connfigurationFilePath = connfigurationFilePath;
        }


        public override void Start()
        {
            var reader = new SettingsReader(_connfigurationFilePath);
            reader.Start();

            Kernel.Bind<IQueueConfiguration>().ToConstant(reader.LoadSection<QueueConfiguration>());
            Kernel.Bind<IAsyncTaskConfiguration>().ToConstant(reader.LoadSection<AsyncTaskConfiguration>());
            Kernel.Bind<IDistributorConfiguration>().ToConstant(reader.LoadSection<DistributorConfiguration>());
            Kernel.Bind<IWriterConfiguration>().ToConstant(reader.LoadSection<WriterConfiguration>());
        }
    }
}
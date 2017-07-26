using Ninject;
using Qoollo.Impl.Common.Server;
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
        
        public ProxyConfiguration ProxyConfiguration { get; protected set; }
        public WriterConfiguration WriterConfiguration { get; protected set; }
        public DistributorConfiguration DistributorConfiguration { get; protected set; }

        public override void Start()
        {
            var reader = new SettingsReader(_connfigurationFilePath);
            reader.Start();

            Kernel.Rebind<IQueueConfiguration>().ToConstant(reader.LoadSection<QueueConfiguration>());
            Kernel.Rebind<IAsyncTaskConfiguration>().ToConstant(reader.LoadSection<AsyncTaskConfiguration>());

            var common = reader.LoadSection<CommonConfiguration>();

            DistributorConfiguration = reader.LoadSection<DistributorConfiguration>();
            Fill(DistributorConfiguration, common);

            WriterConfiguration = reader.LoadSection<WriterConfiguration>();
            Fill(WriterConfiguration, common);

            ProxyConfiguration = reader.LoadSection<ProxyConfiguration>();
            Fill(ProxyConfiguration.NetDistributor, common);

            Kernel.Rebind<IProxyConfiguration>().ToConstant(ProxyConfiguration);
            Kernel.Rebind<IDistributorConfiguration>().ToConstant(DistributorConfiguration);
            Kernel.Rebind<IWriterConfiguration>().ToConstant(WriterConfiguration);
            Kernel.Rebind<ICommonConfiguration>().ToConstant(common);
        }

        private void Fill(WriterConfiguration writer, CommonConfiguration common)
        {
            Fill(writer.NetCollector, common);
            Fill(writer.NetDistributor, common);
        }

        private void Fill(DistributorConfiguration writer, CommonConfiguration common)
        {
            Fill(writer.NetProxy, common);
            Fill(writer.NetWriter, common);
        }

        private void Fill(NetConfiguration config, CommonConfiguration common)
        {
            config.ServiceName = common.Connection.ServiceName;
            config.ServerId = new ServerId(config.Host, config.Port);
        }
    }
}
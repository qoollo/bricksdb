using System;
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
        public CollectorConfiguration CollectorConfiguration { get; protected set; }

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
            Fill(ProxyConfiguration, common);

            CollectorConfiguration = reader.LoadSection<CollectorConfiguration>();
            Fill(CollectorConfiguration);

            Kernel.Rebind<IProxyConfiguration>().ToConstant(ProxyConfiguration);
            Kernel.Rebind<IDistributorConfiguration>().ToConstant(DistributorConfiguration);
            Kernel.Rebind<IWriterConfiguration>().ToConstant(WriterConfiguration);
            Kernel.Rebind<ICollectorConfiguration>().ToConstant(CollectorConfiguration);
            Kernel.Rebind<ICommonConfiguration>().ToConstant(common);
        }

        private void Fill(WriterConfiguration writer, CommonConfiguration common)
        {
            Fill(writer.NetCollector, common);
            Fill(writer.NetDistributor, common);
            Fill(writer.Timeouts.ServersPingMls);
        }

        private void Fill(DistributorConfiguration distributor, CommonConfiguration common)
        {
            Fill(distributor.NetProxy, common);
            Fill(distributor.NetWriter, common);
            Fill(distributor.Timeouts.ServersPingMls);
            Fill(distributor.Timeouts.CheckRestoreMls);
            Fill(distributor.Timeouts.DistributorsPingMls);
            Fill(distributor.Timeouts.UpdateHashMapMls);
        }

        private void Fill(CollectorConfiguration collector)
        {
            Fill(collector.Timeouts.ServersPingMls);
            Fill(collector.Timeouts.DistributorUpdateHashMls);
        }

        private void Fill(ProxyConfiguration proxy, CommonConfiguration common)
        {
            Fill(proxy.NetDistributor, common);
            Fill(proxy.Timeouts.ServersPingMls);
            Fill(proxy.Timeouts.DistributorUpdateInfoMls);
        }

        private void Fill(NetConfiguration config, CommonConfiguration common)
        {
            config.ServiceName = common.Connection.ServiceName;
            config.ServerId = new ServerId(config.Host, config.Port);
        }

        private void Fill(TimeoutConfiguration config)
        {
            config.PeriodTimeSpan = TimeSpan.FromMilliseconds(config.PeriodMls);
        }
    }
}
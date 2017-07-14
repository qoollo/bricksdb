using System;
using System.Diagnostics.Contracts;
using Ninject;
using Ninject.Modules;
using Qoollo.Impl.Collector;
using Qoollo.Impl.Collector.CollectorNet;
using Qoollo.Impl.Collector.Distributor;
using Qoollo.Impl.Collector.Interfaces;
using Qoollo.Impl.Collector.Load;
using Qoollo.Impl.Collector.Model;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Modules.Config;
using Qoollo.Impl.Modules.Interfaces;
using Qoollo.Impl.TestSupport;

namespace Qoollo.Impl.Components
{
    internal class CollectorSystem : ModuleSystemBase
    {
        private readonly ConnectionConfiguration _connectionConfiguration;
        private readonly ConnectionTimeoutConfiguration _connectionTimeoutConfiguration;
        private readonly int _serverPageSize;
        private readonly bool _useHashFile;

        public CollectorSystem(ConnectionConfiguration connectionConfiguration,
            ConnectionTimeoutConfiguration connectionTimeoutConfiguration,
            int serverPageSize, bool useHashFile = true)
        {
            Contract.Requires(connectionConfiguration != null);
            Contract.Requires(connectionTimeoutConfiguration != null);
            Contract.Requires(serverPageSize>0);

            _connectionConfiguration = connectionConfiguration;
            _connectionTimeoutConfiguration = connectionTimeoutConfiguration;
            _serverPageSize = serverPageSize;
            _useHashFile = useHashFile;
        }

        public DistributorModule Distributor { get; private set; }

        public Func<string, ScriptParser,  SearchTaskModule> CreateApi { get; private set; }

        public override void Build(NinjectModule module = null, string configFile = Consts.ConfigFilename)
        {
            module = module ?? new InjectionModule();

            var kernel = new StandardKernel(module);

            var config = new SettingsModule(kernel, configFile);
            config.Start();

            var async = new AsyncTaskModule(kernel);
            kernel.Bind<IAsyncTaskModule>().ToConstant(async);

            var serversModel = new CollectorModel(kernel, _useHashFile);
            kernel.Bind<ICollectorModel>().ToConstant(serversModel);
            serversModel.StartConfig();

            var distributor = new DistributorModule(kernel, new AsyncTasksConfiguration(TimeSpan.FromSeconds(10)));
            kernel.Bind<IDistributorModule>().ToConstant(distributor);

            var net = new CollectorNetModule(kernel, _connectionConfiguration, _connectionTimeoutConfiguration);
            kernel.Bind<ICollectorNetModule>().ToConstant(net);

            var back = new BackgroundModule(kernel);
            var loader = new DataLoader(kernel, net, _serverPageSize, back);

            var searchModule = new SearchTaskCommonModule(kernel);
            CreateApi = searchModule.CreateApi;
            Distributor = distributor;

            AddModule(back);
            AddModule(net);
            AddModule(loader);
            AddModule(distributor);
            AddModule(searchModule);
            AddModule(async);

            AddModuleDispose(async);
            AddModuleDispose(searchModule);
            AddModuleDispose(loader);
            AddModuleDispose(back);
            AddModuleDispose(net);            
            AddModuleDispose(distributor);                 
        }
    }
}

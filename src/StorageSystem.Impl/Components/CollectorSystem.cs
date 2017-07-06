using System;
using System.Diagnostics.Contracts;
using Ninject;
using Ninject.Modules;
using Qoollo.Impl.Collector;
using Qoollo.Impl.Collector.Background;
using Qoollo.Impl.Collector.CollectorNet;
using Qoollo.Impl.Collector.Distributor;
using Qoollo.Impl.Collector.Load;
using Qoollo.Impl.Collector.Model;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.TestSupport;

namespace Qoollo.Impl.Components
{
    internal class CollectorSystem : ModuleSystemBase
    {
        private readonly DistributorHashConfiguration _distributorHashConfiguration;
        private readonly HashMapConfiguration _hashMapConfiguration;
        private readonly ConnectionConfiguration _connectionConfiguration;
        private readonly ConnectionTimeoutConfiguration _connectionTimeoutConfiguration;
        private readonly QueueConfiguration _queueConfiguration;
        private readonly int _serverPageSize;
        private readonly bool _useHashFile;

        public CollectorSystem(DistributorHashConfiguration distributorHashConfiguration,
            HashMapConfiguration hashMapConfiguration,
            ConnectionConfiguration connectionConfiguration,
            ConnectionTimeoutConfiguration connectionTimeoutConfiguration,
            QueueConfiguration queueConfiguration,
            int serverPageSize, bool useHashFile = true)
        {
            Contract.Requires(distributorHashConfiguration!=null);
            Contract.Requires(hashMapConfiguration != null);
            Contract.Requires(connectionConfiguration != null);
            Contract.Requires(connectionTimeoutConfiguration != null);
            Contract.Requires(queueConfiguration != null);
            Contract.Requires(serverPageSize>0);

            _distributorHashConfiguration = distributorHashConfiguration;
            _hashMapConfiguration = hashMapConfiguration;
            _connectionConfiguration = connectionConfiguration;
            _connectionTimeoutConfiguration = connectionTimeoutConfiguration;
            _queueConfiguration = queueConfiguration;
            _serverPageSize = serverPageSize;
            _useHashFile = useHashFile;
        }

        public DistributorModule Distributor { get; private set; }

        public Func<string, ScriptParser,  SearchTaskModule> CreateApi { get; private set; }

        public override void Build(NinjectModule module = null)
        {
            module = module ?? new InjectionModule();

            var kernel = new StandardKernel(module);

            var async = new AsyncTaskModule(kernel, new QueueConfiguration(4, 10));

            var serversModel = new CollectorModel(_distributorHashConfiguration, _hashMapConfiguration, _useHashFile);
            var distributor = new DistributorModule(kernel, serversModel, async,
                new AsyncTasksConfiguration(TimeSpan.FromSeconds(10)));

            var net = new CollectorNetModule(kernel, _connectionConfiguration, _connectionTimeoutConfiguration, distributor);

            distributor.SetNetModule(net);
            
            var back = new BackgroundModule(kernel, _queueConfiguration);
            var loader = new DataLoader(kernel, net, _serverPageSize, back);

            var searchModule = new SearchTaskCommonModule(kernel, loader, distributor, back, serversModel);
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

using System.Diagnostics.Contracts;
using Ninject;
using Ninject.Modules;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DistributorModules;
using Qoollo.Impl.DistributorModules.Caches;
using Qoollo.Impl.DistributorModules.DistributorNet;
using Qoollo.Impl.DistributorModules.Interfaces;
using Qoollo.Impl.DistributorModules.ParallelWork;
using Qoollo.Impl.DistributorModules.Transaction;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Config;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.TestSupport;

namespace Qoollo.Impl.Components
{
    internal class DistributorSystem : ModuleSystemBase
    {
        private readonly DistributorHashConfiguration _distributorHashConfiguration;
        private readonly ConnectionConfiguration _connectionConfiguration;
        private readonly DistributorCacheConfiguration _cacheConfiguration;
        private readonly NetReceiverConfiguration _receiverConfigurationForDb;
        private readonly NetReceiverConfiguration _receiverConfigurationForProxy;
        private readonly HashMapConfiguration _hashMapConfiguration;
        private readonly AsyncTasksConfiguration _pingConfig;
        private readonly AsyncTasksConfiguration _checkConfig;
        private readonly ServerId _localfordb;
        private readonly ServerId _localforproxy;
        private readonly ConnectionTimeoutConfiguration _connectionTimeoutConfiguration;

        public DistributorSystem(ServerId localfordb, ServerId localforproxy,
            DistributorHashConfiguration distributorHashConfiguration,
            ConnectionConfiguration connectionConfiguration,
            DistributorCacheConfiguration cacheConfiguration,
            NetReceiverConfiguration receiverConfigurationForDb,
            NetReceiverConfiguration receiverConfigurationForProxy,
            HashMapConfiguration hashMapConfiguration, AsyncTasksConfiguration pingConfig,
            AsyncTasksConfiguration checkConfig, ConnectionTimeoutConfiguration connectionTimeoutConfiguration)
        {
            Contract.Requires(distributorHashConfiguration != null);
            Contract.Requires(connectionConfiguration != null);
            Contract.Requires(cacheConfiguration != null);
            Contract.Requires(receiverConfigurationForDb != null);
            Contract.Requires(receiverConfigurationForProxy != null);
            Contract.Requires(localfordb != null);
            Contract.Requires(localforproxy != null);
            Contract.Requires(hashMapConfiguration != null);
            Contract.Requires(pingConfig != null);
            Contract.Requires(checkConfig != null);
            _pingConfig = pingConfig;
            _checkConfig = checkConfig;
            _connectionTimeoutConfiguration = connectionTimeoutConfiguration;
            _distributorHashConfiguration = distributorHashConfiguration;
            _hashMapConfiguration = hashMapConfiguration;
            _connectionConfiguration = connectionConfiguration;
            _cacheConfiguration = cacheConfiguration;
            _receiverConfigurationForDb = receiverConfigurationForDb;
            _receiverConfigurationForProxy = receiverConfigurationForProxy;
            _localfordb = localfordb;
            _localforproxy = localforproxy;
        }

        public DistributorModule Distributor { get; private set; }

        protected virtual DistributorNetModule CreateNetModule(StandardKernel kernel, ConnectionConfiguration connectionConfiguration)
        {
            return new DistributorNetModule(kernel, _connectionConfiguration, _connectionTimeoutConfiguration);
        }

        public override void Build(NinjectModule module = null)
        {
            module = module ?? new InjectionModule();
            Kernel = new StandardKernel(module);

            var config = new SettingsModule(Kernel, Consts.ConfigFilename);
            config.Start();

            var q = new GlobalQueue(Kernel);
            Kernel.Bind<IGlobalQueue>().ToConstant(q);

            var cache = new DistributorTimeoutCache(_cacheConfiguration);
            Kernel.Bind<IDistributorTimeoutCache>().ToConstant(cache);

            var net = CreateNetModule(Kernel, _connectionConfiguration);
            Kernel.Bind<IDistributorNetModule>().ToConstant(net);

            var distributor = new DistributorModule(Kernel, _pingConfig, _checkConfig, _distributorHashConfiguration,
                 _localfordb, _localforproxy, _hashMapConfiguration);
            Kernel.Bind<IDistributorModule>().ToConstant(distributor);

            Distributor = distributor;

            var transaction = new TransactionModule(Kernel, 
                _distributorHashConfiguration.CountReplics);
            Kernel.Bind<ITransactionModule>().ToConstant(transaction);

            var main = new MainLogicModule(Kernel);
            Kernel.Bind<IMainLogicModule>().ToConstant(main);
            
            var input = new InputModuleWithParallel(Kernel);
            Kernel.Bind<IInputModule>().ToConstant(input);

            var receive = new NetDistributorReceiver(Kernel, _receiverConfigurationForDb, _receiverConfigurationForProxy);

            AddModule(receive);            
            AddModule(transaction);
            AddModule(input);
            AddModule(main);
            AddModule(net);
            AddModule(distributor);
            AddModule(q);

            AddModuleDispose(receive);
            AddModuleDispose(q);
            AddModuleDispose(input);
            AddModuleDispose(cache);
            AddModuleDispose(main);
            AddModuleDispose(transaction);
            AddModuleDispose(distributor);            
            AddModuleDispose(net);            
        }

        internal StandardKernel Kernel { get; set; }
    }
}

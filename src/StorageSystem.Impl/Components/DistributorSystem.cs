using System.Diagnostics.Contracts;
using Ninject;
using Ninject.Modules;
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
        private readonly DistributorCacheConfiguration _cacheConfiguration;
        private readonly AsyncTasksConfiguration _pingConfig;
        private readonly AsyncTasksConfiguration _checkConfig;

        public DistributorSystem(
            DistributorCacheConfiguration cacheConfiguration,
            AsyncTasksConfiguration pingConfig,
            AsyncTasksConfiguration checkConfig)
        {
            Contract.Requires(cacheConfiguration != null);
            Contract.Requires(pingConfig != null);
            Contract.Requires(checkConfig != null);
            _pingConfig = pingConfig;
            _checkConfig = checkConfig;
            _cacheConfiguration = cacheConfiguration;
        }

        public DistributorModule Distributor { get; private set; }

        protected virtual DistributorNetModule CreateNetModule(StandardKernel kernel)
        {
            return new DistributorNetModule(kernel);
        }

        public override void Build(NinjectModule module = null, string configFile = Consts.ConfigFilename)
        {
            module = module ?? new InjectionModule();
            Kernel = new StandardKernel(module);

            var config = new SettingsModule(Kernel, configFile);
            config.Start();

            var q = new GlobalQueue(Kernel);
            Kernel.Bind<IGlobalQueue>().ToConstant(q);

            var cache = new DistributorTimeoutCache(_cacheConfiguration);
            Kernel.Bind<IDistributorTimeoutCache>().ToConstant(cache);

            var net = CreateNetModule(Kernel);
            Kernel.Bind<IDistributorNetModule>().ToConstant(net);

            var distributor = new DistributorModule(Kernel, _pingConfig, _checkConfig);
            Kernel.Bind<IDistributorModule>().ToConstant(distributor);

            Distributor = distributor;

            var transaction = new TransactionModule(Kernel);
            Kernel.Bind<ITransactionModule>().ToConstant(transaction);

            var main = new MainLogicModule(Kernel);
            Kernel.Bind<IMainLogicModule>().ToConstant(main);
            
            var input = new InputModuleWithParallel(Kernel);
            Kernel.Bind<IInputModule>().ToConstant(input);

            var receive = new NetDistributorReceiver(Kernel);

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

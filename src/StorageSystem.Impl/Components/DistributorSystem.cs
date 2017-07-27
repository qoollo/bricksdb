using Ninject;
using Ninject.Modules;
using Qoollo.Impl.Common.Support;
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

            var cache = new DistributorTimeoutCache(config.DistributorConfiguration.Cache);
            Kernel.Bind<IDistributorTimeoutCache>().ToConstant(cache);

            var net = CreateNetModule(Kernel);
            Kernel.Bind<IDistributorNetModule>().ToConstant(net);

            var distributor = new DistributorModule(Kernel);
            Kernel.Bind<IDistributorModule>().ToConstant(distributor);

            Distributor = distributor;

            var transaction = new TransactionModule(Kernel);
            Kernel.Bind<ITransactionModule>().ToConstant(transaction);

            var main = new MainLogicModule(Kernel);
            Kernel.Bind<IMainLogicModule>().ToConstant(main);
            
            var input = new InputModuleWithParallel(Kernel);
            Kernel.Bind<IInputModule>().ToConstant(input);

            var receive = new NetDistributorReceiver(Kernel);

            AddModule(cache);
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

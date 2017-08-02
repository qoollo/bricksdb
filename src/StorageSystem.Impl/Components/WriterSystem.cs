using Ninject;
using Ninject.Modules;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Modules.Config;
using Qoollo.Impl.Modules.Interfaces;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.TestSupport;
using Qoollo.Impl.Writer;
using Qoollo.Impl.Writer.AsyncDbWorks;
using Qoollo.Impl.Writer.Db;
using Qoollo.Impl.Writer.Interfaces;
using Qoollo.Impl.Writer.WriterNet;

namespace Qoollo.Impl.Components
{
    internal class WriterSystem: ModuleSystemBase
    {
        private readonly bool _isNeedRestore;        

        public WriterSystem(bool isNeedRestore = false)
        {
            _isNeedRestore = isNeedRestore;
        }

        public DistributorModule Distributor { get; private set; }

        public DbModuleCollection DbModule { get; private set; }

        public override void Build(NinjectModule module = null, string configFile = Consts.ConfigFilename)
        {
            module = module ?? new InjectionModule();
            var kernel = new StandardKernel(module);

            var config = new SettingsModule(kernel, configFile);
            config.Start();

            var q = new GlobalQueue(kernel);
            kernel.Bind<IGlobalQueue>().ToConstant(q);

            var db = new DbModuleCollection(kernel);
            kernel.Bind<IDbModule>().ToConstant(db);

            var net = new WriterNetModule(kernel);
            kernel.Bind<IWriterNetModule>().ToConstant(net);

            var async = new AsyncTaskModule(kernel);
            kernel.Bind<IAsyncTaskModule>().ToConstant(async);

            var model = new WriterModel(kernel, config.WriterConfiguration.NetDistributor.ServerId);
            kernel.Bind<IWriterModel>().ToConstant(model);

            var restore = new AsyncDbWorkModule(kernel, _isNeedRestore);
            kernel.Bind<IAsyncDbWorkModule>().ToConstant(restore);

            var distributor = new DistributorModule(kernel);
            kernel.Bind<IDistributorModule>().ToConstant(distributor);

            Distributor = distributor;
            DbModule = db;

            var main = new MainLogicModule(kernel);
            kernel.Bind<IMainLogicModule>().ToConstant(main);

            var input = new InputModule(kernel);
            kernel.Bind<IInputModule>().ToConstant(input);

            var receiver = new NetWriterReceiver(kernel);

            AddModule(model);
            AddModule(net);
            AddModule(distributor);
            AddModule(input);
            AddModule(db);
            AddModule(async);
            AddModule(restore);
            AddModule(main);
            AddModule(receiver);
            AddModule(q);

            AddModuleDispose(receiver);
            AddModuleDispose(restore);
            AddModuleDispose(async);
            AddModuleDispose(q);
            AddModuleDispose(input);
            AddModuleDispose(main);
            AddModuleDispose(distributor);
            AddModuleDispose(db);
            AddModuleDispose(net);
        }
    }
}

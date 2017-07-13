using System.Diagnostics.Contracts;
using Ninject;
using Ninject.Modules;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
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
        private readonly NetReceiverConfiguration _receiverConfigurationForWrite;
        private readonly NetReceiverConfiguration _receiverConfigurationForCollector;
        private readonly HashMapConfiguration _hashMapConfiguration;
        private readonly ConnectionConfiguration _connectionConfiguration;
        private readonly ServerId _local;
        private readonly RestoreModuleConfiguration _transferRestoreConfiguration;
        private readonly RestoreModuleConfiguration _initiatorRestoreConfiguration;
        private readonly RestoreModuleConfiguration _timeoutRestoreConfiguration;
        private readonly bool _isNeedRestore;        
        private readonly ConnectionTimeoutConfiguration _connectionTimeoutConfiguration;

        public WriterSystem(ServerId local,
            NetReceiverConfiguration receiverConfigurationForWrite,
            NetReceiverConfiguration receiverConfigurationForCollector,
            HashMapConfiguration hashMapConfiguration,
            ConnectionConfiguration connectionConfiguration,
            RestoreModuleConfiguration transferRestoreConfiguration,
            RestoreModuleConfiguration initiatorRestoreConfiguration,
            ConnectionTimeoutConfiguration connectionTimeoutConfiguration, 
            RestoreModuleConfiguration timeoutRestoreConfiguration,            
            bool isNeedRestore = false)
        {
            Contract.Requires(local != null);
            Contract.Requires(receiverConfigurationForWrite != null);
            Contract.Requires(receiverConfigurationForCollector != null);
            Contract.Requires(hashMapConfiguration != null);
            Contract.Requires(connectionConfiguration != null);
            Contract.Requires(transferRestoreConfiguration != null);
            Contract.Requires(initiatorRestoreConfiguration != null);

            //_queueConfigurationRestore = queueConfigurationRestore ?? new QueueConfiguration(1, 1000);

            _receiverConfigurationForWrite = receiverConfigurationForWrite;
            _receiverConfigurationForCollector = receiverConfigurationForCollector;
            _hashMapConfiguration = hashMapConfiguration;
            _connectionConfiguration = connectionConfiguration;
            _initiatorRestoreConfiguration = initiatorRestoreConfiguration;
            _connectionTimeoutConfiguration = connectionTimeoutConfiguration;
            _timeoutRestoreConfiguration = timeoutRestoreConfiguration;
            _isNeedRestore = isNeedRestore;
            _transferRestoreConfiguration = transferRestoreConfiguration;
            _local = local;
        }

        public DistributorModule Distributor { get; private set; }

        public DbModuleCollection DbModule { get; private set; }

        public override void Build(NinjectModule module = null)
        {
            module = module ?? new InjectionModule();
            var kernel = new StandardKernel(module);

            var config = new SettingsModule(kernel, Consts.ConfigFilename);
            config.Start();

            var q = new GlobalQueue(kernel);
            kernel.Bind<IGlobalQueue>().ToConstant(q);

            var db = new DbModuleCollection(kernel);
            kernel.Bind<IDbModule>().ToConstant(db);

            var net = new WriterNetModule(kernel, _connectionConfiguration, _connectionTimeoutConfiguration);
            kernel.Bind<IWriterNetModule>().ToConstant(net);

            var async = new AsyncTaskModule(kernel);
            kernel.Bind<IAsyncTaskModule>().ToConstant(async);

            var model = new WriterModel(kernel, _local, _hashMapConfiguration);
            kernel.Bind<IWriterModel>().ToConstant(model);

            var restore = new AsyncDbWorkModule(kernel, _initiatorRestoreConfiguration,
                _transferRestoreConfiguration, _timeoutRestoreConfiguration, _isNeedRestore);
            kernel.Bind<IAsyncDbWorkModule>().ToConstant(restore);

            var distributor = new DistributorModule(kernel);
            kernel.Bind<IDistributorModule>().ToConstant(distributor);

            Distributor = distributor;
            DbModule = db;

            var main = new MainLogicModule(kernel);
            kernel.Bind<IMainLogicModule>().ToConstant(main);

            var input = new InputModule(kernel);
            kernel.Bind<IInputModule>().ToConstant(input);

            var receiver = new NetWriterReceiver(kernel, _receiverConfigurationForWrite,
                _receiverConfigurationForCollector);
                        
            AddModule(model);
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

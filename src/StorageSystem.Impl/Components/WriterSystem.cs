using System.Diagnostics.Contracts;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.Writer;
using Qoollo.Impl.Writer.AsyncDbWorks;
using Qoollo.Impl.Writer.Db;
using Qoollo.Impl.Writer.Distributor;
using Qoollo.Impl.Writer.WriterNet;

namespace Qoollo.Impl.Components
{
    internal class WriterSystem: ModuleSystemBase
    {
        private QueueConfiguration _queueConfiguration;
        private QueueConfiguration _queueConfigurationRestore;
        private NetReceiverConfiguration _receiverConfigurationForWrite;
        private NetReceiverConfiguration _receiverConfigurationForCollector;
        private HashMapConfiguration _hashMapConfiguration;
        private ConnectionConfiguration _connectionConfiguration;
        private ServerId _local;
        private RestoreModuleConfiguration _transferRestoreConfiguration;
        private RestoreModuleConfiguration _initiatorRestoreConfiguration;
        private RestoreModuleConfiguration _timeoutRestoreConfiguration;
        private  bool _isNeedRestore;        
        private ConnectionTimeoutConfiguration _connectionTimeoutConfiguration;

        public WriterSystem(ServerId local, QueueConfiguration queueConfiguration,
            NetReceiverConfiguration receiverConfigurationForWrite,
            NetReceiverConfiguration receiverConfigurationForCollector,
            HashMapConfiguration hashMapConfiguration,
            ConnectionConfiguration connectionConfiguration,
            RestoreModuleConfiguration transferRestoreConfiguration,
            RestoreModuleConfiguration initiatorRestoreConfiguration,
            ConnectionTimeoutConfiguration connectionTimeoutConfiguration, 
            RestoreModuleConfiguration timeoutRestoreConfiguration,            
            bool isNeedRestore = false,
            QueueConfiguration queueConfigurationRestore = null)
        {
            Contract.Requires(local != null);
            Contract.Requires(queueConfiguration != null);
            Contract.Requires(receiverConfigurationForWrite != null);
            Contract.Requires(receiverConfigurationForCollector != null);
            Contract.Requires(hashMapConfiguration != null);
            Contract.Requires(connectionConfiguration != null);
            Contract.Requires(transferRestoreConfiguration != null);
            Contract.Requires(initiatorRestoreConfiguration != null);

            _queueConfigurationRestore = queueConfigurationRestore ?? new QueueConfiguration(1, 1000);

            _queueConfiguration = queueConfiguration;
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

        public override void Build()
        {
            var q = new GlobalQueueInner();
            GlobalQueue.SetQueue(q);

            var db = new DbModuleCollection();

            var net = new WriterNetModule(_connectionConfiguration, _connectionTimeoutConfiguration);

            var async = new AsyncTaskModule(_queueConfiguration);
            var restore = new AsyncDbWorkModule(net, async, db, _initiatorRestoreConfiguration,
                _transferRestoreConfiguration, _timeoutRestoreConfiguration, 
                _queueConfigurationRestore, _local, _isNeedRestore);

            var distributor = new DistributorModule(async, restore, net, _local, _hashMapConfiguration,
                _queueConfiguration, db);

            Distributor = distributor;
            DbModule = db;

            var main = new MainLogicModule(distributor, db);
            var input = new InputModule(main, _queueConfiguration);
            var receiver = new NetWriterReceiver(input, distributor, _receiverConfigurationForWrite,
                _receiverConfigurationForCollector);
                        
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

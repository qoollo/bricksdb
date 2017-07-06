﻿using System.Diagnostics.Contracts;
using Ninject;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.Writer;
using Qoollo.Impl.Writer.AsyncDbWorks;
using Qoollo.Impl.Writer.Db;
using Qoollo.Impl.Writer.WriterNet;

namespace Qoollo.Impl.Components
{
    internal class WriterSystem: ModuleSystemBase
    {
        private readonly QueueConfiguration _queueConfiguration;
        private readonly QueueConfiguration _queueConfigurationRestore;
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

            var kernel = new StandardKernel();

            var db = new DbModuleCollection(kernel);

            var net = new WriterNetModule(kernel, _connectionConfiguration, _connectionTimeoutConfiguration);

            var async = new AsyncTaskModule(kernel, _queueConfiguration);
            var model = new WriterModel(kernel, _local, _hashMapConfiguration);

            var restore = new AsyncDbWorkModule(kernel, model, net, async, db, _initiatorRestoreConfiguration,
                _transferRestoreConfiguration, _timeoutRestoreConfiguration, 
                _queueConfigurationRestore, _isNeedRestore);

            var distributor = new DistributorModule(kernel, model, async, restore, net, _queueConfiguration);

            Distributor = distributor;
            DbModule = db;

            var main = new MainLogicModule(kernel, distributor, db);
            var input = new InputModule(kernel, main, _queueConfiguration);
            var receiver = new NetWriterReceiver(kernel, input, distributor, _receiverConfigurationForWrite,
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

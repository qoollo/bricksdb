﻿using System.Diagnostics.Contracts;
using Ninject;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DistributorModules;
using Qoollo.Impl.DistributorModules.Caches;
using Qoollo.Impl.DistributorModules.DistributorNet;
using Qoollo.Impl.DistributorModules.ParallelWork;
using Qoollo.Impl.DistributorModules.Transaction;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Queue;

namespace Qoollo.Impl.Components
{
    internal class DistributorSystem : ModuleSystemBase
    {
        private readonly DistributorHashConfiguration _distributorHashConfiguration;
        private readonly QueueConfiguration _queueConfiguration;
        private readonly ConnectionConfiguration _connectionConfiguration;
        private readonly DistributorCacheConfiguration _cacheConfiguration;
        private readonly NetReceiverConfiguration _receiverConfigurationForDb;
        private readonly NetReceiverConfiguration _receiverConfigurationForProxy;
        private readonly TransactionConfiguration _transactionConfiguration;
        private readonly HashMapConfiguration _hashMapConfiguration;
        private readonly AsyncTasksConfiguration _pingConfig;
        private readonly AsyncTasksConfiguration _checkConfig;
        private readonly ServerId _localfordb;
        private readonly ServerId _localforproxy;
        private readonly ConnectionTimeoutConfiguration _connectionTimeoutConfiguration;

        public DistributorSystem(ServerId localfordb, ServerId localforproxy,
            DistributorHashConfiguration distributorHashConfiguration,
            QueueConfiguration queueConfiguration,
            ConnectionConfiguration connectionConfiguration,
            DistributorCacheConfiguration cacheConfiguration,
            NetReceiverConfiguration receiverConfigurationForDb,
            NetReceiverConfiguration receiverConfigurationForProxy,
            TransactionConfiguration transactionConfiguration,
            HashMapConfiguration hashMapConfiguration, AsyncTasksConfiguration pingConfig,
            AsyncTasksConfiguration checkConfig, ConnectionTimeoutConfiguration connectionTimeoutConfiguration)
        {
            Contract.Requires(distributorHashConfiguration != null);
            Contract.Requires(_queueConfiguration != null);
            Contract.Requires(connectionConfiguration != null);
            Contract.Requires(cacheConfiguration != null);
            Contract.Requires(receiverConfigurationForDb != null);
            Contract.Requires(receiverConfigurationForProxy != null);
            Contract.Requires(transactionConfiguration != null);
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
            _queueConfiguration = queueConfiguration;
            _connectionConfiguration = connectionConfiguration;
            _cacheConfiguration = cacheConfiguration;
            _receiverConfigurationForDb = receiverConfigurationForDb;
            _receiverConfigurationForProxy = receiverConfigurationForProxy;
            _transactionConfiguration = transactionConfiguration;
            _localfordb = localfordb;
            _localforproxy = localforproxy;
        }

        public DistributorModule Distributor { get; private set; }

        protected virtual DistributorNetModule CreateNetModule(StandardKernel kernel, ConnectionConfiguration connectionConfiguration)
        {
            return new DistributorNetModule(kernel, _connectionConfiguration, _connectionTimeoutConfiguration);
        }

        public override void Build()
        {
            var q = new GlobalQueueInner();
            GlobalQueue.SetQueue(q);

            var kernel = new StandardKernel();

            var cache = new DistributorTimeoutCache(kernel, _cacheConfiguration);
            var net = CreateNetModule(kernel, _connectionConfiguration);
            var distributor = new DistributorModule(kernel, _pingConfig, _checkConfig, _distributorHashConfiguration,
                new QueueConfiguration(1, 1000), net, _localfordb, _localforproxy, _hashMapConfiguration);

            Distributor = distributor;

            net.SetDistributor(distributor);
            var transaction = new TransactionModule(kernel, net, _transactionConfiguration,
                _distributorHashConfiguration.CountReplics, cache);
            var main = new MainLogicModule(kernel, distributor, transaction, cache);
            
            var input = new InputModuleWithParallel(kernel, _queueConfiguration, main, transaction);
            var receive = new NetDistributorReceiver(kernel, main, input, distributor, _receiverConfigurationForDb,
                _receiverConfigurationForProxy);

            AddModule(receive);            
            AddModule(input);
            AddModule(transaction);
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
    }
}

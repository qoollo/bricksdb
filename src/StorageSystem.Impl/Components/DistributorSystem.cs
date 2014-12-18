using System.Diagnostics.Contracts;
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
        private DistributorHashConfiguration _distributorHashConfiguration;
        private QueueConfiguration _queueConfiguration;
        private ConnectionConfiguration _connectionConfiguration;
        private DistributorCacheConfiguration _cacheConfiguration;
        private NetReceiverConfiguration _receiverConfigurationForDb;
        private NetReceiverConfiguration _receiverConfigurationForProxy;
        private TransactionConfiguration _transactionConfiguration;
        private HashMapConfiguration _hashMapConfiguration;
        private AsyncTasksConfiguration _pingConfig;
        private AsyncTasksConfiguration _checkConfig;
        private ServerId _localfordb;
        private ServerId _localforproxy;
        private ConnectionTimeoutConfiguration _connectionTimeoutConfiguration;

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

        protected virtual DistributorNetModule CreateNetModule(ConnectionConfiguration connectionConfiguration)
        {
            return new DistributorNetModule(_connectionConfiguration, _connectionTimeoutConfiguration);
        }

        public override void Build()
        {
            var q = new GlobalQueueInner();
            GlobalQueue.SetQueue(q);

            var cache = new DistributorTimeoutCache(_cacheConfiguration.TimeAliveBeforeDeleteMls,
                _cacheConfiguration.TimeAliveAfterUpdateMls);
            var net = CreateNetModule(_connectionConfiguration);
            var distributor = new DistributorModule(_pingConfig, _checkConfig, _distributorHashConfiguration,
                new QueueConfiguration(1, 1000), net,
                _localfordb, _localforproxy, _hashMapConfiguration);

            Distributor = distributor;

            net.SetDistributor(distributor);
            var transaction = new TransactionModule(_queueConfiguration, net, _transactionConfiguration,
                _distributorHashConfiguration);
            var main = new MainLogicModule(cache, distributor, transaction);

            cache.SetMainLogicModule(main);
            var input = new InputModuleWithParallel(_queueConfiguration, main, transaction);
            var receive = new NetDistributorReceiver(main, input, distributor, _receiverConfigurationForDb,
                _receiverConfigurationForProxy);

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
            AddModuleDispose(distributor);
            AddModuleDispose(net);
        }
    }
}

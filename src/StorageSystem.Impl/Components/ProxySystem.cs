using System;
using System.Diagnostics.Contracts;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.Proxy;
using Qoollo.Impl.Proxy.Caches;
using Qoollo.Impl.Proxy.Input;
using Qoollo.Impl.Proxy.ProxyNet;

namespace Qoollo.Impl.Components
{
    internal class ProxySystem : ModuleSystemBase
    {
        private readonly QueueConfiguration _queueConfiguration;
        private readonly ConnectionConfiguration _connectionConfiguration;
        private readonly ProxyCacheConfiguration _cacheConfiguration;
        private readonly ProxyCacheConfiguration _asyncCacheConfiguration;
        private readonly NetReceiverConfiguration _netReceiverConfiguration;
        private readonly AsyncTasksConfiguration _asyncGetData;
        private readonly AsyncTasksConfiguration _asyncPing;
        private readonly ServerId _local;
        private readonly ConnectionTimeoutConfiguration _connectionTimeoutConfiguration;

        public ProxySystem(ServerId local, QueueConfiguration queueConfiguration,
            ConnectionConfiguration connectionConfiguration,
            ProxyCacheConfiguration cacheConfiguration,
            ProxyCacheConfiguration asyncCacheConfiguration,
            NetReceiverConfiguration receiverConfiguration,
            AsyncTasksConfiguration asyncGetData,
            AsyncTasksConfiguration asyncPing, ConnectionTimeoutConfiguration connectionTimeoutConfiguration)
        {
            Contract.Requires(queueConfiguration != null);
            Contract.Requires(connectionConfiguration != null);
            Contract.Requires(cacheConfiguration != null);
            Contract.Requires(asyncCacheConfiguration != null);
            Contract.Requires(receiverConfiguration != null);
            Contract.Requires(asyncGetData != null);
            Contract.Requires(local != null);
            Contract.Requires(asyncPing != null);

            _local = local;
            _queueConfiguration = queueConfiguration;
            _connectionConfiguration = connectionConfiguration;
            _cacheConfiguration = cacheConfiguration;
            _asyncCacheConfiguration = asyncCacheConfiguration;
            _netReceiverConfiguration = receiverConfiguration;
            _asyncGetData = asyncGetData;
            _asyncPing = asyncPing;
            _connectionTimeoutConfiguration = connectionTimeoutConfiguration;
        }

        public Func<string, bool, IHashCalculater, IStorageInner> CreateApi { get; private set; }

        public override void Build()
        {
            var q = new GlobalQueueInner();
            GlobalQueue.SetQueue(q);

            var asyncCache = new AsyncProxyCache(_asyncCacheConfiguration.TimeAliveSec);

            var net = new ProxyNetModule(_connectionConfiguration, _connectionTimeoutConfiguration);
            var distributor = new ProxyDistributorModule(asyncCache, net, new QueueConfiguration(1, 1000), _local,
                _asyncGetData, _asyncPing);

            net.SetDistributor(distributor);
            var cache = new ProxyCache(_cacheConfiguration.TimeAliveSec);
            var main = new ProxyMainLogicModule(distributor, net, cache);
            var input = new ProxyInputModuleCommon(main, _queueConfiguration, distributor, asyncCache);

            CreateApi = input.CreateApi;

            var receive = new ProxyNetReceiver(distributor, _netReceiverConfiguration);

            AddModule(input);
            AddModule(main);
            AddModule(cache);
            AddModule(asyncCache);
            AddModule(net);
            AddModule(distributor);
            AddModule(receive);
            AddModule(q);

            AddModuleDispose(distributor);
            AddModuleDispose(receive);
            AddModuleDispose(q);
            AddModuleDispose(input);
            AddModuleDispose(asyncCache);
            AddModuleDispose(main);            
            AddModuleDispose(net);
            AddModuleDispose(cache);
        }
    }
}

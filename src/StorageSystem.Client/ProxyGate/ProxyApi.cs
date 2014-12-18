using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Qoollo.Client.Configuration;
using Qoollo.Client.ProxyGate.Handlers;
using Qoollo.Client.Support;
using Qoollo.Impl.Common.Exceptions;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Components;
using Qoollo.Impl.Configurations;
using Consts = Qoollo.Client.Support.Consts;

namespace Qoollo.Client.ProxyGate
{
    public abstract class ProxyApi : IDisposable
    {
        private readonly ProxySystem _proxySystem;
        private readonly Dictionary<string, ProxyHandlerBase> _apis;
        private bool _isBuild;
        private bool _isStarted;
        private bool _isDispose;

        protected ProxyApi(NetConfiguration netConfiguration, ProxyConfiguration proxyConfiguration,
            CommonConfiguration commonConfiguration, TimeoutConfiguration timeoutConfiguration)
        {
            Contract.Requires(netConfiguration != null);
            Contract.Requires(proxyConfiguration != null);
            Contract.Requires(commonConfiguration != null);
            Contract.Requires(timeoutConfiguration != null);

            _isStarted = false;
            _isBuild = false;
            _isDispose = false;

            var server = new ServerId(netConfiguration.Host, netConfiguration.Port);
            var queue = new QueueConfiguration(commonConfiguration.CountThreads, commonConfiguration.QueueSize);

            var connection = new ConnectionConfiguration(netConfiguration.WcfServiceName,
                netConfiguration.CountConnectionsToSingleServer);
            var proxyCacheConfiguration = new ProxyCacheConfiguration(proxyConfiguration.ChangeDistributorTimeoutSec);
            var proxyCacheConfiguration2 = new ProxyCacheConfiguration(proxyConfiguration.SyncOperationsTimeoutSec);
            var netReceiveConfiguration = new NetReceiverConfiguration(netConfiguration.Port, netConfiguration.Host,
                netConfiguration.WcfServiceName);
            var async = new AsyncTasksConfiguration(proxyConfiguration.AsyncUpdateTimeout);
            var ping = new AsyncTasksConfiguration(proxyConfiguration.AsyncPingTimeout);
            var timeout = new ConnectionTimeoutConfiguration(timeoutConfiguration.OpenTimeout,
                timeoutConfiguration.SendTimeout);

            _proxySystem = new ProxySystem(server, queue, connection,
                proxyCacheConfiguration, proxyCacheConfiguration2, netReceiveConfiguration, async, ping, timeout);

            _apis = new Dictionary<string, ProxyHandlerBase>();
        }

        protected ProxyApi(NetConfiguration netConfiguration, ProxyConfiguration proxyConfiguration,
            CommonConfiguration commonConfiguration)
            : this(netConfiguration, proxyConfiguration, commonConfiguration,
                new TimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout))
        {
        }

        protected IStorage<TKey, TValue> CallApi<TKey, TValue>(string tableName)
        {
            ProxyHandlerBase tuple;
            if (_apis.TryGetValue(tableName, out tuple))
            {
                var t = tuple as ProxyHandlerTuple<TKey, TValue>;

                if (_isDispose || !_isStarted)
                    return t.EmptyHandler;

                return t.Handler;
            }

            throw new InitializationException(Errors.TableDoesNotExists);
        }

        protected IStorage<TKey, TValue> RegistrateApi<TKey, TValue>(string tableName, bool hashFromValue,
            IDataProvider<TKey, TValue> dataProvider)
        {
            if (_apis.ContainsKey(tableName))
                throw new InitializationException(Errors.TableAlreadyExists);

            var api = _proxySystem.CreateApi(tableName, hashFromValue, new HashFakeImpl<TKey, TValue>(dataProvider));
            var handler = new ProxyHandler<TKey, TValue>(api, dataProvider);
            var empty = new ProxyHandlerEmpty<TKey, TValue>();

            var tuple = new ProxyHandlerTuple<TKey, TValue>(empty, handler, IsEmpty);

            _apis.Add(tableName, tuple);
            return tuple;
        }

        private bool IsEmpty()
        {
            return _isDispose || !_isStarted;
        }

        protected abstract void InnerBuild();

        [Libs.Logger.LoggerWrapperInitializationMethod]
        public static void Init(Libs.Logger.ILogger innerLogger)
        {
            Libs.Logger.Logger.InitializeLoggerInAssembly(Libs.Logger.Logger.ConsoleLogger,
                typeof (ProxySystem).Assembly);
        }

        public void Build()
        {
            _proxySystem.Build();
            InnerBuild();

            _isBuild = true;
        }

        public void Start()
        {
            if (_isBuild)
            {
                _proxySystem.Start();
                _isStarted = true;
            }
            else
                throw new Exception("System not build");
        }

        public void Dispose()
        {
            if (_isBuild)
            {
                _isDispose = true;
                _proxySystem.Dispose();
            }
            else
                throw new Exception("System not build");
        }
    }
}

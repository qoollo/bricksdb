using System;
using System.Diagnostics.Contracts;
using Qoollo.Client.Configuration;
using Qoollo.Client.Support;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Components;
using Qoollo.Impl.Configurations;

namespace Qoollo.Client.DistributorGate
{
    public class DistributorApi
    {
        private readonly DistributorSystem _distributorSystem;
        private DistributorHandler _handler;
        private DistributorHandlerEmpty _handlerEmpty = new DistributorHandlerEmpty();
        private bool _isBuild;
        private bool _isStarted;
        private bool _isDispose;

        public DistributorApi(DistributorNetConfiguration netConfiguration,
            DistributorConfiguration distributorConfiguration, CommonConfiguration commonConfiguration,
            TimeoutConfiguration timeoutConfiguration)
        {
            Contract.Requires(netConfiguration != null);
            Contract.Requires(distributorConfiguration != null);
            Contract.Requires(commonConfiguration != null);
            Contract.Requires(timeoutConfiguration != null);

            var dbServer = new ServerId( netConfiguration.Host, netConfiguration.PortForStorage);
            var proxyServer = new ServerId( netConfiguration.Host,netConfiguration.PortForProxy);

            var distrHash = new DistributorHashConfiguration(distributorConfiguration.CountReplics);
            var queue = new QueueConfiguration(commonConfiguration.CountThreads, commonConfiguration.QueueSize);
            var connection = new ConnectionConfiguration(netConfiguration.WcfServiceName,
                netConfiguration.CountConnectionsToSingleServer);
            var distrCache = new DistributorCacheConfiguration(distributorConfiguration.DataAliveTime,
                distributorConfiguration.DataAliveAfterUpdate);
            var dbNetReceive = new NetReceiverConfiguration(netConfiguration.PortForStorage,
                netConfiguration.Host, netConfiguration.WcfServiceName);
            var proxyNetReceive = new NetReceiverConfiguration(netConfiguration.PortForProxy,
                netConfiguration.Host, netConfiguration.WcfServiceName);
            var transaction = new TransactionConfiguration(commonConfiguration.CountThreads);
            var hashMap = new HashMapConfiguration(distributorConfiguration.FileWithHashName,
                HashMapCreationMode.ReadFromFile, 1,
                distributorConfiguration.CountReplics, HashFileType.Distributor);
            var asyncPing = new AsyncTasksConfiguration(distributorConfiguration.PingPeriod);
            var asyncCheck = new AsyncTasksConfiguration(distributorConfiguration.CheckPeriod);
            var timeou = new ConnectionTimeoutConfiguration(timeoutConfiguration.OpenTimeout,
                timeoutConfiguration.SendTimeout);

            _distributorSystem = new DistributorSystem(dbServer, proxyServer, distrHash, queue, connection, distrCache,
                dbNetReceive, proxyNetReceive, transaction, hashMap, asyncPing, asyncCheck, timeou);

            _handler = new DistributorHandler(_distributorSystem);
        }

        public DistributorApi(DistributorNetConfiguration netConfiguration,
            DistributorConfiguration distributorConfiguration, CommonConfiguration commonConfiguration)
            : this(
                netConfiguration, distributorConfiguration, commonConfiguration,
                new TimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout))
        {
        }

        public IDistributorApi Api
        {
            get
            {
                if (_isDispose || !_isStarted)
                    return _handlerEmpty;
                return _handler;
            }
        }

        [Qoollo.Logger.LoggerWrapperInitializationMethod]
        public static void Init(Qoollo.Logger.ILogger innerLogger)
        {
            Qoollo.Logger.Initialization.Initializer.InitializeLoggerInAssembly(Qoollo.Logger.LoggerDefault.ConsoleLogger, typeof(DistributorSystem).Assembly);
        }

        public void Build()
        {
            _distributorSystem.Build();
            _isBuild = true;
        }

        public void Start()
        {
            if (_isBuild)
            {
                _distributorSystem.Start();
                _isStarted = true;
            }
            else
                throw new Exception("System not build");
        }

        public void Dispose()
        {
            if (_isBuild)
            {
                _distributorSystem.Dispose();
                _isDispose = true;
            }
            else
                throw new Exception("System not build");
        }
    }
}

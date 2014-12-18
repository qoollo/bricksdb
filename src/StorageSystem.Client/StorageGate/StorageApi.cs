using System;
using System.Diagnostics.Contracts;
using Qoollo.Client.Configuration;
using Qoollo.Client.Request;
using Qoollo.Client.Support;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Components;
using Qoollo.Impl.Configurations;

namespace Qoollo.Client.StorageGate
{
    public class StorageApi : IDisposable
    {
        private readonly DbControllerSystem _dbControllerSystem;
        private readonly StorageHandler _handler;
        private readonly StorageHandlerEmpty _handlerEmpty = new StorageHandlerEmpty();
        private bool _isBuild;
        private bool _isStarted;
        private bool _isDispose;

        public StorageApi(StorageNetConfiguration netConfiguration,
            StorageConfiguration storageConfiguration, CommonConfiguration commonConfiguration,
            TimeoutConfiguration timeoutConfiguration, bool isNeedRestore = false,
            CommonConfiguration restoreConfiguration = null)
        {
            Contract.Requires(netConfiguration != null);
            Contract.Requires(storageConfiguration != null);
            Contract.Requires(commonConfiguration != null);
            Contract.Requires(timeoutConfiguration != null);

            _isStarted = false;
            _isBuild = false;
            _isDispose = false;

            var server = new ServerId(netConfiguration.Host, netConfiguration.PortForDitributor);
            var queue = new QueueConfiguration(commonConfiguration.CountThreads, commonConfiguration.QueueSize);
            var queueRestore = restoreConfiguration == null
                ? new QueueConfiguration(1, 1000)
                : new QueueConfiguration(restoreConfiguration.CountThreads, restoreConfiguration.QueueSize);
            var netReceiveConfiguration = new NetReceiverConfiguration(netConfiguration.PortForDitributor,
                netConfiguration.Host,
                netConfiguration.WcfServiceName);
            var netReceiveConfiguration2 = new NetReceiverConfiguration(netConfiguration.PortForCollector,
                netConfiguration.Host,
                netConfiguration.WcfServiceName);
            var connection = new ConnectionConfiguration(netConfiguration.WcfServiceName,
                netConfiguration.CountConnectionsToSingleServer);
            var hashMap = new HashMapConfiguration(storageConfiguration.FileWithHashName,
                HashMapCreationMode.ReadFromFile, 1,
                storageConfiguration.CountReplics, HashFileType.Controller);
            var restoreTransfer = new RestoreModuleConfiguration(1, storageConfiguration.TimeoutSendAnswerInRestore);
            var restoreInitiator = new RestoreModuleConfiguration(storageConfiguration.CountRetryWaitAnswerInRestore,
                storageConfiguration.TimeoutWaitAnswerInRestore);
            var restoreTimeout = new RestoreModuleConfiguration(-1, storageConfiguration.PeriodStartDelete,
                storageConfiguration.IsForceDelete, storageConfiguration.PeriodDeleteAfterRestore);

            var timeout = new ConnectionTimeoutConfiguration(timeoutConfiguration.OpenTimeout,
                timeoutConfiguration.SendTimeout);

            _dbControllerSystem = new DbControllerSystem(server, queue, netReceiveConfiguration,
                netReceiveConfiguration2, hashMap,
                connection, restoreTransfer, restoreInitiator, timeout, restoreTimeout, isNeedRestore, queueRestore);

            _handler = new StorageHandler(_dbControllerSystem);
        }

        public StorageApi(StorageNetConfiguration netConfiguration, StorageConfiguration storageConfiguration,
            CommonConfiguration commonConfiguration, bool isNeedRestore = false)
            : this(netConfiguration, storageConfiguration, commonConfiguration,
                new TimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout), isNeedRestore)
        {
        }

        public IStorageApi Api
        {
            get
            {
                if (_isDispose || !_isStarted)
                    return _handlerEmpty;
                return _handler;
            }
        }

        [Libs.Logger.LoggerWrapperInitializationMethod]
        public static void Init(Libs.Logger.ILogger innerLogger)
        {
            Libs.Logger.Logger.InitializeLoggerInAssembly(Libs.Logger.Logger.ConsoleLogger,
                typeof (DbControllerSystem).Assembly);
        }

        public RequestDescription AddDbModule(DbFactory factory)
        {
            if (_isBuild)
                return _handler.AddDbModule(factory);

            return _handlerEmpty.AddDbModule(factory);
        }

        public void Build()
        {
            _dbControllerSystem.Build();
            _isBuild = true;
        }

        public void Start()
        {
            if (_isBuild)
            {
                _dbControllerSystem.Start();
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
                _isBuild = false;
                _isStarted = false;
                _dbControllerSystem.Dispose();
            }
            else
                throw new Exception("System not build");
        }
    }
}

using System;
using System.Diagnostics.Contracts;
using Qoollo.Client.Configuration;
using Qoollo.Client.Request;
using Qoollo.Client.Support;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Components;
using Qoollo.Impl.Configurations;

namespace Qoollo.Client.WriterGate
{
    public class WriterApi : IDisposable
    {
        private readonly WriterSystem _writerSystem;
        private readonly WriterHandler _handler;
        private readonly WriterHandlerEmpty _handlerEmpty = new WriterHandlerEmpty();
        private bool _isBuild;
        private bool _isStarted;
        private bool _isDispose;

        public WriterApi(StorageNetConfiguration netConfiguration,
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
                storageConfiguration.CountReplics, HashFileType.Writer);
            var restoreTransfer = new RestoreModuleConfiguration(1, storageConfiguration.TimeoutSendAnswerInRestore);
            var restoreInitiator = new RestoreModuleConfiguration(storageConfiguration.CountRetryWaitAnswerInRestore,
                storageConfiguration.TimeoutWaitAnswerInRestore);
            var restoreTimeout = new RestoreModuleConfiguration(-1, storageConfiguration.PeriodStartDelete,
                storageConfiguration.IsForceDelete, storageConfiguration.PeriodDeleteAfterRestore);

            var timeout = new ConnectionTimeoutConfiguration(timeoutConfiguration.OpenTimeout,
                timeoutConfiguration.SendTimeout);

            _writerSystem = new WriterSystem(server, queue, netReceiveConfiguration,
                netReceiveConfiguration2, hashMap,
                connection, restoreTransfer, restoreInitiator, timeout, restoreTimeout, isNeedRestore, queueRestore);

            _handler = new WriterHandler(_writerSystem);
        }

        public WriterApi(StorageNetConfiguration netConfiguration, StorageConfiguration storageConfiguration,
            CommonConfiguration commonConfiguration, bool isNeedRestore = false)
            : this(netConfiguration, storageConfiguration, commonConfiguration,
                new TimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout), isNeedRestore)
        {
        }

        public IWriterApi Api
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
            Qoollo.Logger.Initialization.Initializer.InitializeLoggerInAssembly(Qoollo.Logger.LoggerDefault.ConsoleLogger,
                typeof (WriterSystem).Assembly);
        }

        public RequestDescription AddDbModule(DbFactory factory)
        {
            if (_isBuild)
                return _handler.AddDbModule(factory);

            return _handlerEmpty.AddDbModule(factory);
        }

        public void Build()
        {
            _writerSystem.Build();
            _isBuild = true;
        }

        public void Start()
        {
            if (_isBuild)
            {
                _writerSystem.Start();
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
                _writerSystem.Dispose();
            }
            else
                throw new Exception("System not build");
        }
    }
}

using System;
using Qoollo.Client.Request;
using Qoollo.Impl.Components;
using Qoollo.Impl.TestSupport;

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

        internal InjectionModule Module = null;

        public WriterApi(bool isNeedRestore = false)
        {
            _isStarted = false;
            _isBuild = false;
            _isDispose = false;

            _writerSystem = new WriterSystem(isNeedRestore);

            _handler = new WriterHandler(_writerSystem);
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
            _writerSystem.Build(Module);
            _isBuild = true;
        }

        internal void Build(string configFile)
        {
            _writerSystem.Build(Module, configFile);
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

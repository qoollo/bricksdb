using System;
using Qoollo.Impl.Components;
using Qoollo.Impl.TestSupport;

namespace Qoollo.Client.DistributorGate
{
    public class DistributorApi
    {
        private readonly DistributorSystem _distributorSystem;
        private readonly DistributorHandler _handler;
        private readonly DistributorHandlerEmpty _handlerEmpty = new DistributorHandlerEmpty();
        private bool _isBuild;
        private bool _isStarted;
        private bool _isDispose;

        internal InjectionModule Module = null;

        public DistributorApi()
        {
            _distributorSystem = new DistributorSystem();

            _handler = new DistributorHandler(_distributorSystem);
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
            _distributorSystem.Build(Module);
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

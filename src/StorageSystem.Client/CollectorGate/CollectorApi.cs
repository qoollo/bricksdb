using System;
using System.Collections.Generic;
using Qoollo.Client.CollectorGate.Handlers;
using Qoollo.Client.WriterGate;
using Qoollo.Impl.Common.Exceptions;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Components;
using Qoollo.Impl.TestSupport;

namespace Qoollo.Client.CollectorGate
{
    public abstract class CollectorApi
    {
        private readonly CollectorSystem _collectorSystem;
        private readonly Dictionary<string, CollectorHandlerTuple> _apis; 
        private bool _isBuild;
        private bool _isStarted;
        private bool _isDispose;

        internal InjectionModule Module = null;

        protected CollectorApi()
        {
            _isStarted = false;
            _isBuild = false;
            _isDispose = false;

            _collectorSystem = new CollectorSystem();

            _apis = new Dictionary<string, CollectorHandlerTuple>();
        }

        protected ICollectorApi CallApi(string tableName)
        {
            CollectorHandlerTuple tuple;
            if (_apis.TryGetValue(tableName, out tuple))
            {
                if (_isDispose || !_isStarted)
                    return tuple.CollectorHandlerEmpty;

                return tuple.CollectorHandler;
            }

            throw new InitializationException(Errors.TableDoesNotExists);
        }

        protected ICollectorApi RegistrateApi(string tableName, DbFactory factory)
        {
            if (_apis.ContainsKey(tableName))
                throw new InitializationException(Errors.TableAlreadyExists);

            var api = _collectorSystem.CreateApi(tableName, factory.GetParser());
            var handler = new CollectorHandler(api, _collectorSystem);
            var empty = new CollectorHandlerEmpty();

            var tuple = new CollectorHandlerTuple(empty, handler, IsEmpty);
            _apis.Add(tableName, tuple);

            return tuple;
        }

        private bool IsEmpty()
        {
            return _isDispose || !_isStarted;
        }

        [Qoollo.Logger.LoggerWrapperInitializationMethod]
        public static void Init(Qoollo.Logger.ILogger innerLogger)
        {
            Qoollo.Logger.Initialization.Initializer.InitializeLoggerInAssembly(Qoollo.Logger.LoggerDefault.ConsoleLogger, typeof(CollectorSystem).Assembly);
        }

        protected abstract void InnerBuild();

        public void Build()
        {
            _collectorSystem.Build(Module);
            InnerBuild();

            _isBuild = true;
        }

        public void Build(string configFile)
        {
            _collectorSystem.Build(Module, configFile);
            InnerBuild();

            _isBuild = true;
        }

        public void Start()
        {
            if (_isBuild)
            {
                _collectorSystem.Start();
                _isStarted = true;
            }
            else
                throw new Exception("System not build");
        }

        public void Dispose()
        {
            if (_isBuild)
            {
                _collectorSystem.Dispose();
                _isDispose = true;
            }
            else
                throw new Exception("System not build");
        }
    }
}

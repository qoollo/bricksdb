using System;
using System.Collections.Generic;
using Qoollo.Client.ProxyGate.Handlers;
using Qoollo.Client.Support;
using Qoollo.Impl.Common.Exceptions;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Components;
using Qoollo.Impl.TestSupport;

namespace Qoollo.Client.ProxyGate
{
    public abstract class ProxyApi : IDisposable
    {
        private readonly ProxySystem _proxySystem;
        private readonly Dictionary<string, ProxyHandlerBase> _apis;
        private bool _isBuild;
        private bool _isStarted;
        private bool _isDispose;

        internal InjectionModule Module = null;

        protected ProxyApi()
        {

            _isStarted = false;
            _isBuild = false;
            _isDispose = false;

            _proxySystem = new ProxySystem();

            _apis = new Dictionary<string, ProxyHandlerBase>();
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

        [Qoollo.Logger.LoggerWrapperInitializationMethod]
        public static void Init(Qoollo.Logger.ILogger innerLogger)
        {
            Qoollo.Logger.Initialization.Initializer.InitializeLoggerInAssembly(Qoollo.Logger.LoggerDefault.ConsoleLogger,
                typeof (ProxySystem).Assembly);
        }

        public void Build()
        {
            _proxySystem.Build(Module);
            InnerBuild();

            _isBuild = true;
        }

        public void Build(string configFile)
        {
            _proxySystem.Build(Module, configFile);
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

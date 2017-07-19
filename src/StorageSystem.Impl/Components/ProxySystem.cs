using System;
using System.Diagnostics.Contracts;
using Ninject;
using Ninject.Modules;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules;
using Qoollo.Impl.Modules.Config;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.Proxy;
using Qoollo.Impl.Proxy.Caches;
using Qoollo.Impl.Proxy.Input;
using Qoollo.Impl.Proxy.Interfaces;
using Qoollo.Impl.Proxy.ProxyNet;
using Qoollo.Impl.TestSupport;

namespace Qoollo.Impl.Components
{
    internal class ProxySystem : ModuleSystemBase
    {
        private readonly ProxyCacheConfiguration _cacheConfiguration;
        private readonly ProxyCacheConfiguration _asyncCacheConfiguration;
        private readonly AsyncTasksConfiguration _asyncGetData;
        private readonly AsyncTasksConfiguration _asyncPing;

        public ProxySystem(
            ProxyCacheConfiguration cacheConfiguration,
            ProxyCacheConfiguration asyncCacheConfiguration,
            AsyncTasksConfiguration asyncGetData,
            AsyncTasksConfiguration asyncPing)
        {
            Contract.Requires(cacheConfiguration != null);
            Contract.Requires(asyncCacheConfiguration != null);
            Contract.Requires(asyncGetData != null);
            Contract.Requires(asyncPing != null);

            _cacheConfiguration = cacheConfiguration;
            _asyncCacheConfiguration = asyncCacheConfiguration;
            _asyncGetData = asyncGetData;
            _asyncPing = asyncPing;
        }

        public Func<string, bool, IHashCalculater, IStorageInner> CreateApi { get; private set; }

        public override void Build(NinjectModule module = null, string configFile = Consts.ConfigFilename)
        {
            module = module ?? new InjectionModule();
            var kernel = new StandardKernel(module);

            var config = new SettingsModule(kernel, configFile);
            config.Start();

            var q = new GlobalQueue(kernel);
            kernel.Bind<IGlobalQueue>().ToConstant(q);

            var asyncCache = new AsyncProxyCache(_asyncCacheConfiguration.TimeAliveSec);
            kernel.Bind<IAsyncProxyCache>().ToConstant(asyncCache);

            var net = new ProxyNetModule(kernel);
            kernel.Bind<IProxyNetModule>().ToConstant(net);

            var distributor = new ProxyDistributorModule(kernel, _asyncGetData, _asyncPing);
            kernel.Bind<IProxyDistributorModule>().ToConstant(distributor);

            var cache = new ProxyCache(_cacheConfiguration.TimeAliveSec);
            kernel.Bind<IProxyCache>().ToConstant(cache);

            var main = new ProxyMainLogicModule(kernel);
            kernel.Bind<IProxyMainLogicModule>().ToConstant(main);

            var input = new ProxyInputModuleCommon(kernel);
            kernel.Bind<IProxyInputModuleCommon>().ToConstant(input);

            CreateApi = input.CreateApi;

            var receive = new ProxyNetReceiver(kernel, config.ProxyConfiguration.NetDistributor);

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

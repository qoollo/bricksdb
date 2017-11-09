using System;
using Ninject;
using Ninject.Modules;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Impl.Common.Support;
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
        public Func<string, bool, IHashCalculater, IStorageInner> CreateApi { get; private set; }

        public override void Build(NinjectModule module = null, string configFile = Consts.ConfigFilename)
        {
            module = module ?? new InjectionModule();
            var kernel = new StandardKernel(module);

            var config = new SettingsModule(kernel, configFile);
            config.Start();

            var q = new GlobalQueue(kernel);
            kernel.Bind<IGlobalQueue>().ToConstant(q);

            var asyncCache = new AsyncProxyCache(config.ProxyConfiguration.Cache);
            kernel.Bind<IAsyncProxyCache>().ToConstant(asyncCache);

            var net = new ProxyNetModule(kernel);
            kernel.Bind<IProxyNetModule>().ToConstant(net);

            var distributor = new ProxyDistributorModule(kernel);
            kernel.Bind<IProxyDistributorModule>().ToConstant(distributor);

            var cache = new ProxyCache(config.ProxyConfiguration.Cache);
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

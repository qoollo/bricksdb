﻿using System.Linq;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Components;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.Proxy;

namespace Qoollo.Tests.TestProxy
{
    class TestProxySystem : ProxySystem
    {
        public TestProxySystem(
            ProxyCacheConfiguration cacheConfiguration,
            ProxyCacheConfiguration asyncCacheConfiguration,
            AsyncTasksConfiguration asyncGetData, AsyncTasksConfiguration asyncPing)
            : base( cacheConfiguration, asyncCacheConfiguration, asyncGetData, asyncPing)
        {
        }

        public GlobalQueue Queue
        {
            get { return Modules.First(x => x is GlobalQueue) as GlobalQueue; }
        }

        public ProxyDistributorModule Distributor
        {
            get { return Modules.First(x => x is ProxyDistributorModule) as ProxyDistributorModule; }
        }

        public UserTransaction GetTransaction(UserTransaction transaction)
        {
            var module = Modules.First(x => x is ProxyMainLogicModule) as ProxyMainLogicModule;
            return module.GetTransaction(transaction);
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Client.Configuration;
using Qoollo.Client.DistributorGate;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestModules;

namespace Qoollo.Tests
{
    [TestClass]
    public class TestBase
    {
        internal TestWriterGate _writer1;
        internal TestWriterGate _writer2;
        internal TestWriterGate _writer3;
        internal DistributorApi _distr;
        internal TestGate _proxy;
        internal TestDistributorGate _distrTest;
        internal const int distrServer1 = 22323;
        internal const int distrServer2 = 22423;
        internal const int proxyServer = 22331;
        internal const int distrServer12 = 22324;
        internal const int distrServer22 = 22424;
        internal const int storageServer1 = 22357;
        internal const int storageServer2 = 22156;
        internal const int storageServer3 = 22157;

        [TestInitialize]
        public void Initialize()
        {
            var common = new CommonConfiguration(1, 100);
            var distrNet = new DistributorNetConfiguration("localhost",
                distrServer1, distrServer12, "testService", 10);
            var distrConf = new DistributorConfiguration(1, "TestRestore",
                //TimeSpan.FromMilliseconds(10000000), TimeSpan.FromMilliseconds(500000), TimeSpan.FromMinutes(100),
                TimeSpan.FromMilliseconds(10000000), TimeSpan.FromMilliseconds(500000), TimeSpan.FromSeconds(100),
                TimeSpan.FromMilliseconds(10000000));

            _distr = new DistributorApi(distrNet, distrConf, common);
            _distr.Build();

            var netconfig = new NetConfiguration("localhost", proxyServer, "testService", 10);
            var toconfig = new ProxyConfiguration(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));

            _proxy = new TestGate(netconfig, toconfig, common);
            _proxy.Build();

            _distrTest = new TestDistributorGate();
            _writer1 = new TestWriterGate();
            _writer2 = new TestWriterGate();
            _writer3 = new TestWriterGate();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading;
using Ninject;
using Qoollo.Client.Configuration;
using Qoollo.Client.DistributorGate;
using Qoollo.Client.Support;
using Qoollo.Client.WriterGate;
using Qoollo.Impl.Collector.Model;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Components;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DistributorModules;
using Qoollo.Impl.DistributorModules.DistributorNet;
using Qoollo.Impl.DistributorModules.Model;
using Qoollo.Impl.Proxy;
using Qoollo.Impl.Proxy.Caches;
using Qoollo.Impl.Proxy.ProxyNet;
using Qoollo.Impl.TestSupport;
using Qoollo.Tests.NetMock;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestModules;
using Qoollo.Tests.TestProxy;

namespace Qoollo.Tests
{
    public class TestBase:IDisposable
    {
        internal  StandardKernel _kernel = new StandardKernel(new TestInjectionModule());

        internal TestWriterGate _writer1;
        internal TestWriterGate _writer2;
        internal TestWriterGate _writer3;
        internal TestGate _proxy;
        internal TestDistributorGate _distrTest;
        internal const int distrServer1 = 22323;
        internal const int distrServer2 = 22423;
        internal const int proxyServer = 22331;
        internal const int distrServer12 = 22324;
        internal const int distrServer22 = 22424;
        internal const int storageServer1 = 22155;
        internal const int storageServer2 = 22156;
        internal const int storageServer3 = 22157;
        internal const int storageServer4 = 22158;

        internal string file1 = "restoreHelp1.txt";
        internal string file2 = "restoreHelp2.txt";
        internal string file3 = "restoreHelp3.txt";
        internal string file4 = "restoreFile4.txt";

        private readonly List<int> _writerPorts;

        private static readonly object Lock = new object();
        internal ConnectionConfiguration ConnectionConfiguration;
        internal QueueConfiguration QueueConfiguration;
        internal CommonConfiguration CommonConfiguration;

        public TestBase()
        {
            Monitor.Enter(Lock);

            NetMock.NetMock.Instance = new NetMock.NetMock();

            InitInjection.RestoreUsePackage = false;
            InitInjection.RestoreHelpFileOut = Impl.Common.Support.Consts.RestoreHelpFile;

            _writerPorts = new List<int> {storageServer1, storageServer2, storageServer3, storageServer4};

            ConnectionConfiguration = new ConnectionConfiguration("testService", 10);
            QueueConfiguration = new QueueConfiguration(1, 100);

            CommonConfiguration = new CommonConfiguration(1, 100);
            var netconfig = new NetConfiguration("localhost", proxyServer, "testService", 10);
            var toconfig = new ProxyConfiguration(TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(10),
                TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));

            _proxy = new TestGate(netconfig, toconfig, CommonConfiguration);
            _proxy.Module = new TestInjectionModule();
            _proxy.Build();

            _distrTest = new TestDistributorGate();
            _writer1 = new TestWriterGate();
            _writer2 = new TestWriterGate();
            _writer3 = new TestWriterGate();
        }

        protected void CreateHashFile(string filename, int countServers)
        {
            var writer = new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, countServers, 3, HashFileType.Distributor));
            writer.CreateMap();
            for (int i = 0; i < countServers; i++)
            {
                writer.SetServer(i, "localhost", _writerPorts[i], 157);
            }
            writer.Save();
        }

        internal ServerId ServerId(int serverPort)
        {
            return new ServerId("localhost", serverPort);
        }

        internal ProxyNetModule ProxyNetModule()
        {
            return new ProxyNetModule(_kernel, ConnectionConfiguration,
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
        }

        internal DistributorNetModule DistributorNetModule()
        {
            var connection = new ConnectionConfiguration("testService", 10);
            return new DistributorNetModule(_kernel, connection, new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
        }

        internal DistributorModule DistributorDistributorModule(string filename, int countReplics,
            DistributorNetModule net, int pingTo = 200, int asyncCheckTo = 2000,
            int distrPort1 = distrServer1, int distrPort2 = distrServer12)
        {
            return new DistributorModule(_kernel, 
                new AsyncTasksConfiguration(TimeSpan.FromMilliseconds(pingTo)),
                new AsyncTasksConfiguration(TimeSpan.FromMilliseconds(asyncCheckTo)),
                new DistributorHashConfiguration(countReplics),
                QueueConfiguration, net,
                new ServerId("localhost", distrPort1),
                new ServerId("localhost", distrPort2),
                new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Distributor));
        }

        internal ProxyDistributorModule ProxyDistributorModule(ProxyNetModule net, int proxyPort)
        {
            return new ProxyDistributorModule(_kernel, new AsyncProxyCache(TimeSpan.FromMinutes(100)),
                net, QueueConfiguration, ServerId(proxyPort),
                new AsyncTasksConfiguration(TimeSpan.FromDays(1)),
                new AsyncTasksConfiguration(TimeSpan.FromDays(1)));
        }

        internal CollectorModel CollectorModel(string filename, int countReplics)
        {
            return new CollectorModel(new DistributorHashConfiguration(countReplics),
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, countReplics, 1,
                        HashFileType.Writer));
        }

        internal TestGate TestGate(int proxyPort, int syncTo = 60)
        {
            var netconfig = new NetConfiguration("localhost", proxyPort, "testService", 10);
            var toconfig = new ProxyConfiguration(TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(syncTo),
                TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));

            return new TestGate(netconfig, toconfig, CommonConfiguration);
        }

        internal DistributorConfiguration DistributorConfiguration(string filename, int countReplics)
        {
            return new DistributorConfiguration(countReplics, filename, TimeSpan.FromMilliseconds(100000),
                    TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(10000));
        }

        internal DistributorApi DistributorApi(DistributorConfiguration distrConf, int portForProxy, int portForStorage)
        {
            var distrNet = new DistributorNetConfiguration("localhost", portForProxy, portForStorage, "testService", 10);
            return new DistributorApi(distrNet, distrConf, CommonConfiguration);
        }

        internal StorageConfiguration StorageConfiguration(string filename, int countReplics, 
            int restoreAnswerMls = 10000000, int deleteRestoreMls = 1000000, int periodStartDelete = 1000000, 
            bool isForceDelete = false)
        {
            return new StorageConfiguration(filename, countReplics, 10, TimeSpan.FromHours(1),
                TimeSpan.FromMilliseconds(restoreAnswerMls), 
                TimeSpan.FromMilliseconds(deleteRestoreMls), 
                TimeSpan.FromMilliseconds(periodStartDelete), isForceDelete);
        }

        internal WriterApi WriterApi(StorageConfiguration storageConfiguration, int portForDistr, int portForCollector = 157)
        {
            var storageNet = new StorageNetConfiguration("localhost", portForDistr, portForCollector, "testService", 10);
            return new WriterApi(storageNet, storageConfiguration, CommonConfiguration);
        }

        internal NetReceiverConfiguration NetReceiverConfiguration(int serverPort)
        {
            return new NetReceiverConfiguration(serverPort, "localhost", "testService");
        }

        internal DistributorCacheConfiguration DistributorCacheConfiguration(int deleteMls = 2000, int updateMls = 200000)
        {
            return new DistributorCacheConfiguration(TimeSpan.FromMilliseconds(deleteMls), TimeSpan.FromMilliseconds(updateMls));
        }

        internal WriterSystemModel WriterSystemModel(string filename, int countReplics)
        {
            return new WriterSystemModel(new DistributorHashConfiguration(countReplics),
                new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, countReplics,
                    HashFileType.Distributor));
        }

        internal TestProxySystem TestProxySystem(int proxyPort, int cacheToSec = 2, int asyncCacheToSec = 2)
        {
            var pcc = new ProxyCacheConfiguration(TimeSpan.FromSeconds(cacheToSec));
            var pcc2 = new ProxyCacheConfiguration(TimeSpan.FromSeconds(asyncCacheToSec));
            return new TestProxySystem(ServerId(proxyPort),
               QueueConfiguration, ConnectionConfiguration, 
               pcc, pcc2,
               NetReceiverConfiguration(proxyPort),
               new AsyncTasksConfiguration(new TimeSpan()),
               new AsyncTasksConfiguration(new TimeSpan()),
               new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
        }

        internal DistributorSystem DistributorSystem(DistributorCacheConfiguration cacheConfiguration,
            string filename, int countReplics, int portForProxy, int portForWriter,
            int toMls1 = 200, int toMls2 = 30000)
        {
            return new DistributorSystem(ServerId(portForWriter), ServerId(portForProxy),
                new DistributorHashConfiguration(countReplics),
                QueueConfiguration, ConnectionConfiguration, cacheConfiguration,
                NetReceiverConfiguration(portForWriter),
                NetReceiverConfiguration(portForProxy),
                new TransactionConfiguration(1),
                new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, countReplics,
                    HashFileType.Distributor),
                new AsyncTasksConfiguration(TimeSpan.FromMilliseconds(toMls1)),
                new AsyncTasksConfiguration(TimeSpan.FromMilliseconds(toMls2)),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
        }

        internal WriterSystem WriterSystem(string filename, int countReplics, int portForDistr, int portForCollector = 157)
        {
            return new WriterSystem(ServerId(portForDistr), QueueConfiguration,
                NetReceiverConfiguration(portForDistr),
                NetReceiverConfiguration(portForCollector),
                new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, countReplics, HashFileType.Writer),
                ConnectionConfiguration,
                new RestoreModuleConfiguration(10, new TimeSpan()),
                new RestoreModuleConfiguration(10, new TimeSpan()),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout),
                new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)));
        }

        protected virtual void Dispose(bool isUserCall)
        {
        }

        public void Dispose()
        {
            Dispose(true);
            Monitor.Exit(Lock);
        }
    }
}

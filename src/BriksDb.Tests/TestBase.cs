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
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DistributorModules;
using Qoollo.Impl.DistributorModules.DistributorNet;
using Qoollo.Impl.TestSupport;
using Qoollo.Tests.NetMock;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestModules;

namespace Qoollo.Tests
{
    public class TestBase:IDisposable
    {
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

        private readonly List<int> _writerPorts;

        private static readonly object Lock = new object();
        internal ConnectionConfiguration ConnectionConfiguration;
        internal QueueConfiguration QueueConfiguration;
        internal CommonConfiguration CommonConfiguration;

        public TestBase()
        {
            Monitor.Enter(Lock);

            InitInjection.Kernel = new StandardKernel(new TestInjectionModule());

            _writerPorts = new List<int> {storageServer1, storageServer2, storageServer3, storageServer4};

            ConnectionConfiguration = new ConnectionConfiguration("testService", 10);
            QueueConfiguration = new QueueConfiguration(1, 100);

            CommonConfiguration = new CommonConfiguration(1, 100);
            var netconfig = new NetConfiguration("localhost", proxyServer, "testService", 10);
            var toconfig = new ProxyConfiguration(TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(10),
                TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));

            _proxy = new TestGate(netconfig, toconfig, CommonConfiguration);
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

        internal DistributorNetModule DistributorNetModule()
        {
            var connection = new ConnectionConfiguration("testService", 10);
            return new DistributorNetModule(connection, new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
        }

        internal DistributorModule DistributorDistributorModule(string filename, int countReplics,
            DistributorNetModule net, int pingTo = 200, int asyncCheckTo = 2000)
        {
            return new DistributorModule(
                new AsyncTasksConfiguration(TimeSpan.FromMilliseconds(pingTo)),
                new AsyncTasksConfiguration(TimeSpan.FromMilliseconds(asyncCheckTo)),
                new DistributorHashConfiguration(countReplics),
                QueueConfiguration,
                net,
                new ServerId("localhost", distrServer1),
                new ServerId("localhost", distrServer12),
                new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Distributor));
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

        internal StorageConfiguration StorageConfiguration(string filename, int countReplics)
        {
            return new StorageConfiguration(filename, countReplics, 10, TimeSpan.FromHours(1), TimeSpan.FromHours(1),
                    TimeSpan.FromHours(1), TimeSpan.FromHours(1), false);
        }

        internal WriterApi WriterApi(StorageConfiguration storageConfiguration, int portForDistr, int portForCollector)
        {
            var storageNet = new StorageNetConfiguration("localhost", portForDistr, portForCollector, "testService", 10);
            return new WriterApi(storageNet, storageConfiguration, CommonConfiguration);
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

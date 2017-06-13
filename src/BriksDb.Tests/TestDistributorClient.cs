using System;
using System.Threading;
using Ninject;
using Qoollo.Client.Configuration;
using Qoollo.Client.DistributorGate;
using Qoollo.Client.Request;
using Qoollo.Client.WriterGate;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.TestSupport;
using Qoollo.Tests.NetMock;
using Qoollo.Tests.TestModules;
using Qoollo.Tests.TestWriter;
using Xunit;

namespace Qoollo.Tests
{
    public class TestDistributorClient
    {
        public TestDistributorClient()
        {
            InitInjection.Kernel = new StandardKernel(new TestInjectionModule());
        }

        [Fact]
        public void DistributorApi_ProcessAsyncOperationsFromProxy()
        {
            const int proxyServer = 22213;
            const int distrServer1 = 22214;
            const int distrServer12 = 22215;
            const int storageServer = 22216;

            #region hell

            var writer = new HashWriter(new HashMapConfiguration("TestClientDistributor", HashMapCreationMode.CreateNew, 1, 1, HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer, 157);
            writer.Save();

            var netconfig = new NetConfiguration("localhost", proxyServer, "testService", 10);
            var toconfig = new ProxyConfiguration(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
            var common = new CommonConfiguration(1, 100);

            var proxy = new TestGate(netconfig, toconfig, common);

            var distrNet = new DistributorNetConfiguration("localhost",
                distrServer1, distrServer12, "testService", 10);
            var distrConf = new DistributorConfiguration(1, "TestClientDistributor",
                TimeSpan.FromMilliseconds(100000), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(10000));

            var distr = new DistributorApi(distrNet, distrConf, common);

            var storageNet = new StorageNetConfiguration("localhost", storageServer, 157, "testService", 10);
            var storageConfig = new StorageConfiguration("TestClientDistributor", 1, 10, TimeSpan.FromHours(1), TimeSpan.FromHours(1),
                TimeSpan.FromHours(1), TimeSpan.FromHours(1), false);

            var storage = new WriterApi(storageNet, storageConfig, common);

            #endregion

            proxy.Build();
            proxy.Start();

            distr.Build();
            distr.Start();

            proxy.Int.SayIAmHere("localhost", distrServer1);

            storage.Build();
            storage.AddDbModule(new TestInMemoryDbFactory());
            storage.Start();

            const int count = 50;

            for (int i = 0; i < count; i++)
            {
                var state = proxy.Int.CreateSync(i, i);
                Assert.Equal(RequestState.Complete, state.State);
            }

            for (int i = 0; i < count; i++)
            {
                RequestDescription description;
                var read = proxy.Int.Read(i, out description);

                Assert.Equal(i, read);
                Assert.Equal(RequestState.Complete, description.State);
            }

            for (int i = 0; i < count; i++)
            {
                var state = proxy.Int.DeleteSync(i);
                Assert.Equal(RequestState.Complete, state.State);
            }

            for (int i = 0; i < count; i++)
            {
                RequestDescription description;
                proxy.Int.Read(i, out description);

                Assert.Equal(RequestState.DataNotFound, description.State);
            }

            proxy.Dispose();
            distr.Dispose();
            storage.Dispose();
        }

        [Fact]
        public void DistributorApi_ProcessSyncOperationsFromProxy()
        {
            const int proxyServer = 22327;
            const int distrServer1 = 22328;
            const int distrServer12 = 22329;
            const int storageServer = 22330;

            #region hell

            var writer = new HashWriter(new HashMapConfiguration("TestClientDistributor", HashMapCreationMode.CreateNew, 1, 1, HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer, 157);
            writer.Save();

            var netconfig = new NetConfiguration("localhost", proxyServer, "testService", 10);
            var toconfig = new ProxyConfiguration(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
            var common = new CommonConfiguration(1, 100);

            var proxy = new TestGate(netconfig, toconfig, common);

            var distrNet = new DistributorNetConfiguration("localhost",
                distrServer1, distrServer12, "testService", 10);
            var distrConf = new DistributorConfiguration(1, "TestClientDistributor",
                TimeSpan.FromMilliseconds(100000), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(10000));

            var distr = new DistributorApi(distrNet, distrConf, common);

            var storageNet = new StorageNetConfiguration("localhost", storageServer, 157, "testService", 10);
            var storageConfig = new StorageConfiguration("TestClientDistributor", 1, 10, TimeSpan.FromHours(1), TimeSpan.FromHours(1),
                TimeSpan.FromHours(1), TimeSpan.FromHours(1), false);

            var storage = new WriterApi(storageNet, storageConfig, common);

            #endregion

            proxy.Build();
            proxy.Start();

            distr.Build();
            distr.Start();

            proxy.Int.SayIAmHere("localhost", distrServer1);

            storage.Build();
            storage.AddDbModule(new TestInMemoryDbFactory());
            storage.Start();

            const int count = 50;

            for (int i = 0; i < count; i++)
            {
                proxy.Int.CreateAsync(i, i);
            }

            Thread.Sleep(2000);

            for (int i = 0; i < count; i++)
            {
                RequestDescription description;
                var read = proxy.Int.Read(i, out description);

                Assert.Equal(i, read);
                Assert.Equal(RequestState.Complete, description.State);
            }

            for (int i = 0; i < count; i++)
            {
                proxy.Int.DeleteAsync(i);
            }

            Thread.Sleep(2000);

            for (int i = 0; i < count; i++)
            {
                RequestDescription description;
                proxy.Int.Read(i, out description);

                Assert.Equal(RequestState.DataNotFound, description.State);
            }

            proxy.Dispose();
            distr.Dispose();
            storage.Dispose();
        }
    }
}

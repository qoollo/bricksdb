using System;
using System.Threading.Tasks;
using Ninject;
using Qoollo.Client.Configuration;
using Qoollo.Client.DistributorGate;
using Qoollo.Client.Request;
using Qoollo.Client.Support;
using Qoollo.Client.WriterGate;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Components;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.TestSupport;
using Qoollo.Tests.NetMock;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestModules;
using Qoollo.Tests.TestWriter;
using Xunit;

namespace Qoollo.Tests
{
    [Collection("test collection 1")]
    public class TestProxyClient: TestBase
    {
        new TestGate _proxy;

        public TestProxyClient():base()
        {
            InitInjection.Kernel = new StandardKernel(new TestInjectionModule());

            const int proxyServer = 22369;
            var netconfig = new NetConfiguration("localhost", proxyServer, "testService", 10);
            var toconfig = new ProxyConfiguration(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
            var common = new CommonConfiguration(1, 100);

            _proxy = new TestGate(netconfig, toconfig, common);
            _proxy.Build();
            _proxy.Start();
        }

        protected override void Dispose(bool isUserCall)
        {
            _proxy.Dispose();
        }

        [Fact]        
        public void ClientProxy_CrudOperations()
        {
            const int distrServer1 = 22206;
            const int distrServer12 = 22207;
            const int storageServer = 22208;

            var filename = nameof(ClientProxy_CrudOperations);
            using (new FileCleaner(filename))
            using (new FileCleaner(Impl.Common.Support.Consts.RestoreHelpFile))
            {
                #region hell

                var writer = new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 1, 1, HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer, 157);
                writer.Save();

                var distrHash = new DistributorHashConfiguration(1);
                var queue = new QueueConfiguration(1, 100);
                var connection = new ConnectionConfiguration("testService", 10);
                var distrCache = new DistributorCacheConfiguration(TimeSpan.FromMilliseconds(10000), TimeSpan.FromMilliseconds(1000000));
                var netReceive1 = new NetReceiverConfiguration(distrServer1, "localhost", "testService");
                var netReceive12 = new NetReceiverConfiguration(distrServer12, "localhost", "testService");
                var trans = new TransactionConfiguration(1);
                var hashMap = new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Writer);
                var async = new AsyncTasksConfiguration(TimeSpan.FromSeconds(10));

                var distr = new DistributorSystem(new ServerId("localhost", distrServer1),
                    new ServerId("localhost", distrServer12),
                    distrHash, queue, connection, distrCache, netReceive1, netReceive12, trans,
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Distributor), 
                    async, async,
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));

                var netReceivedb = new NetReceiverConfiguration(storageServer, "localhost", "testService");
                var restore = new RestoreModuleConfiguration(4, TimeSpan.FromMinutes(1));

                var storage = new WriterSystem(new ServerId("localhost", storageServer), queue,
                    netReceivedb, new NetReceiverConfiguration(1, "fake", "fake"), hashMap, connection, restore, restore,
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout),
                    new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)));

                #endregion

                distr.Build();
                distr.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer12);

                storage.Build();
                storage.DbModule.AddDbModule(new TestDbInMemory());
                storage.Start();

                const int count = 50;

                for (int i = 0; i < count; i++)
                {
                    var state = _proxy.Int.CreateSync(i, i);
                    Assert.Equal(RequestState.Complete, state.State);
                }

                for (int i = 0; i < count; i++)
                {
                    RequestDescription description;
                    var read = _proxy.Int.Read(i, out description);

                    Assert.Equal(i, read);
                    Assert.Equal(RequestState.Complete, description.State);
                }

                for (int i = 0; i < count; i++)
                {
                    var state = _proxy.Int.DeleteSync(i);
                    Assert.Equal(RequestState.Complete, state.State);
                }

                for (int i = 0; i < count; i++)
                {
                    RequestDescription description;
                    _proxy.Int.Read(i, out description);

                    Assert.Equal(RequestState.DataNotFound, description.State);
                }

                distr.Dispose();
                storage.Dispose();
            }
        }
        
        [Fact]
        public void ClientProxy_AsyncRead()
        {
            const int distrServer1 = 22359;
            const int distrServer12 = 22360;
            const int storageServer = 22361;

            var filename = nameof(ClientProxy_AsyncRead);
            using (new FileCleaner(filename))
            using (new FileCleaner(Impl.Common.Support.Consts.RestoreHelpFile))
            {
                #region hell

                var writer = new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 1, 1, HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer, 157);
                writer.Save();

                var distrHash = new DistributorHashConfiguration(1);
                var queue = new QueueConfiguration(1, 100);
                var connection = new ConnectionConfiguration("testService", 10);
                var distrCache = new DistributorCacheConfiguration(TimeSpan.FromMilliseconds(10000), TimeSpan.FromMilliseconds(1000000));
                var netReceive1 = new NetReceiverConfiguration(distrServer1, "localhost", "testService");
                var netReceive12 = new NetReceiverConfiguration(distrServer12, "localhost", "testService");
                var trans = new TransactionConfiguration(1);
                var hashMap = new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Writer);
                var async = new AsyncTasksConfiguration(TimeSpan.FromSeconds(10));

                var distr = new DistributorSystem(new ServerId("localhost", distrServer1),
                    new ServerId("localhost", distrServer12),
                    distrHash, queue, connection, distrCache, netReceive1, netReceive12, trans,
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Distributor), 
                    async, async,
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));

                var netReceivedb = new NetReceiverConfiguration(storageServer, "localhost", "testService");
                var restore = new RestoreModuleConfiguration(4, TimeSpan.FromMinutes(1));

                var storage = new WriterSystem(new ServerId("localhost", storageServer), queue,
                    netReceivedb, new NetReceiverConfiguration(1, "fake", "fake"), hashMap, connection, restore, restore,
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout),
                    new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)));

                #endregion

                distr.Build();
                distr.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer12);

                storage.Build();
                storage.DbModule.AddDbModule(new TestDbInMemory());
                storage.Start();

                const int count = 50;

                for (int i = 0; i < count; i++)
                {
                    var state = _proxy.Int.CreateSync(i, i);

                    Assert.Equal(RequestState.Complete, state.State);
                }

                for (int i = 0; i < count; i++)
                {
                    var read = _proxy.Int.ReadAsync(i);

                    read.Wait();

                    Assert.Equal(i, read.Result.Value);
                    Assert.Equal(RequestState.Complete, read.Result.RequestDescription.State);
                }

                for (int i = 0; i < count; i++)
                {
                    var state = _proxy.Int.DeleteSync(i);
                    Assert.Equal(RequestState.Complete, state.State);
                }

                for (int i = 0; i < count; i++)
                {
                    var read = _proxy.Int.ReadAsync(i);

                    read.Wait();
                    Assert.Equal(RequestState.DataNotFound, read.Result.RequestDescription.State);
                }

                distr.Dispose();
                storage.Dispose();
            }
        }

        [Fact]
        public void ClientProxy_Dispose_DisposeWhenWriting()
        {           
            const int distrServer1 = 22370;
            const int distrServer12 = 22371;
            const int storageServer = 22372;

            var filename = nameof(ClientProxy_Dispose_DisposeWhenWriting);
            using (new FileCleaner(filename))
            using (new FileCleaner(Impl.Common.Support.Consts.RestoreHelpFile))
            {
                #region hell

                var writer = new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 1, 1, HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer, 157);
                writer.Save();

                var common = new CommonConfiguration(1, 100);

                var distr =
                    new DistributorApi(
                        new DistributorNetConfiguration("localhost", distrServer1, distrServer12, "testService"),
                        new DistributorConfiguration(1, filename), common);


                var storage = new WriterApi(
                    new StorageNetConfiguration("localhost", storageServer, 157, "testService"),
                    new StorageConfiguration(1, filename),
                    new CommonConfiguration());

                #endregion

                distr.Build();
                distr.Start();

                var result = _proxy.Int.SayIAmHere("localhost", distrServer1);
                Assert.Equal(RequestState.Complete, result.State);

                storage.Build();
                storage.AddDbModule(new TestInMemoryDbFactory());
                storage.Start();

                const int count = 500;

                for (int i = 0; i < count; i++)
                {
                    if (i == count / 4)
                        Task.Run(() => _proxy.Dispose());

                    var state = _proxy.Int.CreateSync(i, i);

                    if (!(state.State == RequestState.Complete || state.State == RequestState.Error &&
                          state.ErrorDescription == "System disposed" || i == count / 4))
                        Assert.NotNull(null);
                }

                distr.Dispose();
                storage.Dispose();
            }
        }
    }
}

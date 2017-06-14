using System;
using System.Threading;
using Ninject;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Components;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.TestSupport;
using Qoollo.Tests.NetMock;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestProxy;
using Qoollo.Tests.TestWriter;
using Xunit;

namespace Qoollo.Tests
{
    [Collection("test collection 1")]
    public class TestProxyAndDistributor:TestBase
    {
        new TestProxySystem _proxy;
        new const int proxyServer = 32223;

        public TestProxyAndDistributor():base()
        {
            InitInjection.Kernel = new StandardKernel(new TestInjectionModule());

            var queue = new QueueConfiguration(1, 100);
            var connection = new ConnectionConfiguration("testService", 10);
            var ndrc2 = new NetReceiverConfiguration(proxyServer, "localhost", "testService");
            var pcc = new ProxyCacheConfiguration(TimeSpan.FromSeconds(2));
            _proxy = new TestProxySystem(new ServerId("localhost", proxyServer),
               queue, connection, pcc, pcc, ndrc2,
               new AsyncTasksConfiguration(new TimeSpan()),
               new AsyncTasksConfiguration(new TimeSpan()),
               new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));

            _proxy.Build();
        }

        [Fact]
        public void ProxyAndDistributor_Create_WriterMock()
        {
            var filename = nameof(ProxyAndDistributor_Create_WriterMock);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                var writer =
                    new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 1, 3,
                        HashFileType.Collector));
                writer.CreateMap();
                writer.SetServer(0, "localhost", 21181, 157);
                writer.Save();

                var dhc = new DistributorHashConfiguration(1);
                var queue = new QueueConfiguration(1, 100);
                var connection = new ConnectionConfiguration("testService", 10);
                var dcc = new DistributorCacheConfiguration(TimeSpan.FromMilliseconds(600), TimeSpan.FromMilliseconds(1000));
                var ndrc = new NetReceiverConfiguration(22222, "localhost", "testService");
                var ndrc12 = new NetReceiverConfiguration(23222, "localhost", "testService");

                var distr = new DistributorSystem(new ServerId("localhost", 22222),
                    new ServerId("localhost", 23222),
                    dhc, queue, connection, dcc, ndrc, ndrc12,
                    new TransactionConfiguration(1),
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Distributor),
                    new AsyncTasksConfiguration(TimeSpan.FromMilliseconds(200)),
                    new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));

                var server = new ServerId("localhost", 23222);
                try
                {
                    distr.Build();

                    _proxy.Start();
                    distr.Start();

                    GlobalQueue.Queue.Start();

                    _proxy.Distributor.SayIAmHere(server);

                    var api = _proxy.CreateApi("", false, new StoredDataHashCalculator());

                    var transaction = api.Create(10, TestHelper.CreateStoredData(10));
                    Assert.NotNull(transaction);
                    Thread.Sleep(200);
                    transaction = _proxy.GetTransaction(transaction);
                    Assert.NotNull(transaction);
                    Thread.Sleep(4000);
                    transaction = _proxy.GetTransaction(transaction);
                    Assert.NotNull(transaction);
                    Assert.Equal(TransactionState.DontExist, transaction.State);

                    var server1 = new ServerId("localhost", 21181);
                    var netconfig = new ConnectionConfiguration("testService", 1);
                    var s = TestHelper.OpenWriterHost(server1.Port);

                    Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                    transaction = api.Create(11, TestHelper.CreateStoredData(11));
                    Assert.NotNull(transaction);
                    Thread.Sleep(200);
                    transaction = _proxy.GetTransaction(transaction);
                    GlobalQueue.Queue.TransactionQueue.Add(new Transaction(transaction));
                    Thread.Sleep(100);
                    transaction = _proxy.GetTransaction(transaction);
                    Assert.NotNull(transaction);
                    if (transaction.State == TransactionState.TransactionInProcess)
                    {
                        Thread.Sleep(100);
                        transaction = _proxy.GetTransaction(transaction);
                    }
                    Assert.Equal(TransactionState.Complete, transaction.State);
                    Thread.Sleep(1000);
                    transaction = _proxy.GetTransaction(transaction);
                    Assert.NotNull(transaction);
                    Assert.Equal(TransactionState.DontExist, transaction.State);
                    s.Dispose();
                }
                finally
                {
                    _proxy.Dispose();
                    distr.Dispose();
                }
            }
        }

        [Fact]
        public void ProxyAndDistributor_Create_WriterMock_TwoReplics()
        {
            var filename1 = nameof(ProxyAndDistributor_Create_WriterMock_TwoReplics) +"1";
            var filename2 = nameof(ProxyAndDistributor_Create_WriterMock_TwoReplics) +"2";
            using (new FileCleaner(filename1))
            using (new FileCleaner(filename2))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                var writer = new HashWriter(new HashMapConfiguration(filename1, HashMapCreationMode.CreateNew, 2, 3, HashFileType.Writer));
                writer.CreateMap();
                writer.SetServer(0, "localhost", 21191, 157);
                writer.SetServer(1, "localhost", 21192, 157);
                writer.Save();

                writer = new HashWriter(new HashMapConfiguration(filename2, HashMapCreationMode.CreateNew, 2, 3, HashFileType.Writer));
                writer.CreateMap();
                writer.SetServer(0, "localhost", 21193, 157);
                writer.SetServer(1, "localhost", 21192, 157);
                writer.Save();

                var dhc = new DistributorHashConfiguration(2);
                var queue = new QueueConfiguration(1, 100);
                var connection = new ConnectionConfiguration("testService", 10);
                var dcc = new DistributorCacheConfiguration(TimeSpan.FromMilliseconds(400), TimeSpan.FromMilliseconds(1000));
                var ndrc = new NetReceiverConfiguration(22223, "localhost", "testService");
                var ndrc12 = new NetReceiverConfiguration(23223, "localhost", "testService");
                var ndrc2 = new NetReceiverConfiguration(22224, "localhost", "testService");
                var ndrc22 = new NetReceiverConfiguration(23224, "localhost", "testService");

                var distr = new DistributorSystem(new ServerId("localhost", 22223),
                    new ServerId("localhost", 23223),
                    dhc, queue, connection, dcc, ndrc, ndrc12,
                    new TransactionConfiguration(1),
                    new HashMapConfiguration(filename1, HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Distributor),
                    new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                    new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
                var distr2 = new DistributorSystem(new ServerId("localhost", 22224),
                    new ServerId("localhost", 23224),
                    dhc, queue, connection, dcc, ndrc2, ndrc22,
                    new TransactionConfiguration(1),
                    new HashMapConfiguration(filename2, HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Distributor),
                    new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                    new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));


                var ser = new ServerId("localhost", 23223);
                var ser2 = new ServerId("localhost", 23224);

                try
                {
                    distr.Build();
                    distr2.Build();

                    _proxy.Start();
                    distr.Start();
                    distr2.Start();

                    _proxy.Distributor.SayIAmHere(ser);
                    _proxy.Distributor.SayIAmHere(ser2);

                    var server1 = new ServerId("localhost", 21191);
                    var server2 = new ServerId("localhost", 21192);
                    var server3 = new ServerId("localhost", 21193);
                    var netconfig = new ConnectionConfiguration("testService", 1);
                    var s1 = TestHelper.OpenWriterHost(server1.Port);
                    var s2 = TestHelper.OpenWriterHost(server2.Port);
                    var s3 = TestHelper.OpenWriterHost(server3.Port);

                    Thread.Sleep(TimeSpan.FromMilliseconds(300));

                    var api = _proxy.CreateApi("", false, new StoredDataHashCalculator());

                    api.Create(10, TestHelper.CreateStoredData(10));
                    api.Create(11, TestHelper.CreateStoredData(11));
                    Thread.Sleep(TimeSpan.FromMilliseconds(1000));
                    Assert.Equal(1, s1.Value);
                    Assert.Equal(2, s2.Value);
                    Assert.Equal(1, s3.Value);

                    s1.Dispose();
                    s2.Dispose();
                    s3.Dispose();
                }
                finally
                {
                    _proxy.Dispose();
                    distr.Dispose();
                    distr2.Dispose();
                }
            }
        }

        [Fact]
        public void ProxyAndDistributor_Read_DirectReadFromOneServerMock()
        {
            const int storageServer = 22261;

            var filename = nameof(ProxyAndDistributor_Read_DirectReadFromOneServerMock);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                var writer = new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 1, 3, HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer, 157);
                writer.Save();

                var dhc = new DistributorHashConfiguration(1);
                var queue = new QueueConfiguration(1, 100);
                var connection = new ConnectionConfiguration("testService", 10);
                var dcc = new DistributorCacheConfiguration(TimeSpan.FromMilliseconds(600), TimeSpan.FromMilliseconds(1000));
                var ndrc = new NetReceiverConfiguration(22260, "localhost", "testService");
                var ndrc12 = new NetReceiverConfiguration(23260, "localhost", "testService");

                var distr = new DistributorSystem(new ServerId("localhost", 22260),
                    new ServerId("localhost", 23260),
                    dhc, queue, connection, dcc, ndrc, ndrc12,
                    new TransactionConfiguration(1),
                    new HashMapConfiguration(filename,HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Distributor),
                    new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                    new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));

                var server = new ServerId("localhost", 23260);

                distr.Build();

                _proxy.Start();
                distr.Start();

                GlobalQueue.Queue.Start();

                _proxy.Distributor.SayIAmHere(server);

                var s = TestHelper.OpenWriterHost(storageServer);

                s.retData = TestHelper.CreateEvent(new StoredDataHashCalculator(), 10);

                var api = _proxy.CreateApi("Event", false, new StoredDataHashCalculator());

                UserTransaction transaction;
                var read = (StoredData)api.Read(10, out transaction);

                Assert.Equal(10, read.Id);
                _proxy.Dispose();
                distr.Dispose();
                s.Dispose();
            }
        }

        [Fact]
        public void ProxyAndDistributor_Read_DirectReadFromOneServer()
        {
            const int storageServer1 = 22462;            
            const int distrServerForProxy = 23263;
            const int distrServerForDb = 22263;

            var filename = nameof(ProxyAndDistributor_Read_DirectReadFromOneServer);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                var writer = new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 1, 3, HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.Save();

                var dhc = new DistributorHashConfiguration(1);
                var queue = new QueueConfiguration(1, 100);
                var connection = new ConnectionConfiguration("testService", 10);
                var dcc = new DistributorCacheConfiguration(TimeSpan.FromMilliseconds(600), TimeSpan.FromMilliseconds(1000));
                var ndrc = new NetReceiverConfiguration(distrServerForDb, "localhost", "testService");
                var ndrc12 = new NetReceiverConfiguration(distrServerForProxy, "localhost", "testService");

                var distr = new DistributorSystem(new ServerId("localhost", distrServerForDb),
                    new ServerId("localhost", distrServerForProxy),
                    dhc, queue, connection, dcc, ndrc, ndrc12,
                    new TransactionConfiguration(1),
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Distributor),
                    new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                    new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));

                var server = new ServerId("localhost", distrServerForProxy);

                var storage = new WriterSystem(new ServerId("localhost", storageServer1), queue,
                    new NetReceiverConfiguration(storageServer1, "localhost", "testService"), 
                    new NetReceiverConfiguration(1, "fake", "fake"),
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Writer),
                    connection, new RestoreModuleConfiguration(10, new TimeSpan()),
                    new RestoreModuleConfiguration(10, new TimeSpan()),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout),
                    new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)));

                storage.Build();
                distr.Build();

                storage.DbModule.AddDbModule(new TestDbInMemory());

                storage.Start();
                _proxy.Start();
                distr.Start();

                GlobalQueue.Queue.Start();

                _proxy.Distributor.SayIAmHere(server);

                const int count = 50;

                var api = _proxy.CreateApi("Int", false, new IntHashConvertor());

                for (int i = 1; i < count; i++)
                {
                    var task = api.CreateSync(i, i);
                    task.Wait();
                    Assert.Equal(TransactionState.Complete, task.Result.State);
                }

                for (int i = 1; i < count; i++)
                {
                    UserTransaction transaction;
                    var read = (int)api.Read(i, out transaction);

                    Assert.Equal(i, read);
                }

                _proxy.Dispose();
                distr.Dispose();
                storage.Dispose();
            }
        }

        [Fact]
        public void ProxyAndDistributor_Read_DirectReadFromTwoServer()
        {
            const int storageServer1 = 22262;
            const int storageServer2 = 22264;            
            const int distrServerForProxy = 23563;
            const int distrServerForDb = 22563;

            var filename = nameof(ProxyAndDistributor_Read_DirectReadFromTwoServer);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                var writer = new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 2, 3, HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                var dhc = new DistributorHashConfiguration(1);
                var queue = new QueueConfiguration(1, 100);
                var connection = new ConnectionConfiguration("testService", 10);
                var dcc = new DistributorCacheConfiguration(TimeSpan.FromMilliseconds(600), TimeSpan.FromMilliseconds(1000));
                var ndrc = new NetReceiverConfiguration(distrServerForDb, "localhost", "testService");
                var ndrc12 = new NetReceiverConfiguration(distrServerForProxy, "localhost", "testService");

                var distr = new DistributorSystem(new ServerId("localhost", distrServerForDb),
                    new ServerId("localhost", distrServerForProxy),
                    dhc, queue, connection, dcc, ndrc, ndrc12,
                    new TransactionConfiguration(1),
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Distributor),
                    new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                    new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));

                var server = new ServerId("localhost", distrServerForProxy);

                var storage1 = new WriterSystem(new ServerId("localhost", storageServer1), queue,
                    new NetReceiverConfiguration(storageServer1, "localhost", "testService")
                    , new NetReceiverConfiguration(1, "fake", "fake"),
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Writer),
                    connection, new RestoreModuleConfiguration(10, new TimeSpan()),
                    new RestoreModuleConfiguration(10, new TimeSpan()),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout),
                    new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)));

                var storage2 = new WriterSystem(new ServerId("localhost", storageServer2), queue,
                    new NetReceiverConfiguration(storageServer2, "localhost", "testService")
                    , new NetReceiverConfiguration(1, "fake", "fake"),
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Writer),
                    connection, new RestoreModuleConfiguration(10, new TimeSpan()),
                    new RestoreModuleConfiguration(10, new TimeSpan()),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout),
                    new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)));

                storage1.Build();
                storage2.Build();
                distr.Build();

                storage1.DbModule.AddDbModule(new TestDbInMemory());
                storage2.DbModule.AddDbModule(new TestDbInMemory());

                storage1.Start();
                storage2.Start();
                _proxy.Start();
                distr.Start();

                GlobalQueue.Queue.Start();

                _proxy.Distributor.SayIAmHere(server);

                const int count = 50;

                var api = _proxy.CreateApi("Int", false, new IntHashConvertor());

                for (int i = 1; i < count; i++)
                {
                    var task = api.CreateSync(i, i);
                    task.Wait();
                    Assert.Equal(TransactionState.Complete, task.Result.State);
                }

                for (int i = 1; i < count; i++)
                {
                    UserTransaction transaction;
                    var read = (int)api.Read(i, out transaction);

                    //  var read = api.Read(i, out transaction);
                    //  Thread.Sleep(10000000);

                    Assert.Equal(i, read);
                }

                _proxy.Dispose();
                distr.Dispose();
                storage1.Dispose();
                storage2.Dispose();
            }
        }

        [Fact]
        public void ProxyAndDistributor_Read_DirectReadFromTwoServer_TwoReplics()
        {
            const int storageServer1 = 22265;
            const int storageServer2 = 22266;            
            const int distrServerForProxy = 23267;
            const int distrServerForDb = 22267;

            var filename = nameof(ProxyAndDistributor_Read_DirectReadFromTwoServer_TwoReplics);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                var writer = new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 2, 3, HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                var dhc = new DistributorHashConfiguration(2);
                var queue = new QueueConfiguration(1, 100);
                var connection = new ConnectionConfiguration("testService", 10);
                var dcc = new DistributorCacheConfiguration(TimeSpan.FromMilliseconds(600000), TimeSpan.FromMilliseconds(1000000));
                var ndrc = new NetReceiverConfiguration(distrServerForDb, "localhost", "testService");
                var ndrc12 = new NetReceiverConfiguration(distrServerForProxy, "localhost", "testService");

                var distr = new DistributorSystem(new ServerId("localhost", distrServerForDb),
                    new ServerId("localhost", distrServerForProxy),
                    dhc, queue, connection, dcc, ndrc, ndrc12,
                    new TransactionConfiguration(1),
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 2, HashFileType.Distributor),
                    new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                    new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));

                var server = new ServerId("localhost", distrServerForProxy);

                var storage1 = new WriterSystem(new ServerId("localhost", storageServer1), queue,
                    new NetReceiverConfiguration(storageServer1, "localhost", "testService")
                    , new NetReceiverConfiguration(1, "fake", "fake"),
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Writer),
                    connection, new RestoreModuleConfiguration(10, new TimeSpan()),
                    new RestoreModuleConfiguration(10, new TimeSpan()),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout),
                    new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)));

                var storage2 = new WriterSystem(new ServerId("localhost", storageServer2), queue,
                    new NetReceiverConfiguration(storageServer2, "localhost", "testService")
                    , new NetReceiverConfiguration(1, "fake", "fake"),
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Writer),
                    connection, new RestoreModuleConfiguration(10, new TimeSpan()),
                    new RestoreModuleConfiguration(10, new TimeSpan()),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout),
                    new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)));

                storage1.Build();
                storage2.Build();
                distr.Build();

                storage1.DbModule.AddDbModule(new TestDbInMemory());
                storage2.DbModule.AddDbModule(new TestDbInMemory());

                storage1.Start();
                storage2.Start();
                _proxy.Start();
                distr.Start();

                GlobalQueue.Queue.Start();

                _proxy.Distributor.SayIAmHere(server);

                const int count = 50;

                Thread.Sleep(100);
                var api = _proxy.CreateApi("Int", false, new IntHashConvertor());

                for (int i = 1; i < count; i++)
                {
                    var task = api.CreateSync(i, i);
                    task.Wait();
                    Assert.Equal(TransactionState.Complete, task.Result.State);
                }

                for (int i = 1; i < count; i++)
                {
                    UserTransaction transaction;
                    var read = (int)api.Read(i, out transaction);

                    Assert.Equal(i, read);
                }

                _proxy.Dispose();
                distr.Dispose();
                storage1.Dispose();
                storage2.Dispose();
            }
        }

        [Fact]
        public void ProxyAndDistributor_Read_DirectReadFromTwoServer_TwoReplics_NoData()
        {
            const int storageServer1 = 22268;
            const int storageServer2 = 22269;            
            const int distrServerForProxy = 23270;
            const int distrServerForDb = 22270;

            var filename = nameof(ProxyAndDistributor_Read_DirectReadFromTwoServer_TwoReplics_NoData);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                var writer = new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 2, 3, HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                var dhc = new DistributorHashConfiguration(2);
                var queue = new QueueConfiguration(1, 100);
                var connection = new ConnectionConfiguration("testService", 10);
                var dcc = new DistributorCacheConfiguration(TimeSpan.FromMilliseconds(600), TimeSpan.FromMilliseconds(1000));
                var ndrc = new NetReceiverConfiguration(distrServerForDb, "localhost", "testService");
                var ndrc12 = new NetReceiverConfiguration(distrServerForProxy, "localhost", "testService");

                var distr = new DistributorSystem(new ServerId("localhost", distrServerForDb),
                    new ServerId("localhost", distrServerForProxy),
                    dhc, queue, connection, dcc, ndrc, ndrc12,
                    new TransactionConfiguration(1),
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 2, HashFileType.Distributor),
                    new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                    new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));

                var server = new ServerId("localhost", distrServerForProxy);

                var storage1 = new WriterSystem(new ServerId("localhost", storageServer1), queue,
                    new NetReceiverConfiguration(storageServer1, "localhost", "testService")
                    , new NetReceiverConfiguration(1, "fake", "fake"),
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Writer),
                    connection, new RestoreModuleConfiguration(10, new TimeSpan()),
                    new RestoreModuleConfiguration(10, new TimeSpan()),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout),
                    new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)));

                var storage2 = new WriterSystem(new ServerId("localhost", storageServer2), queue,
                    new NetReceiverConfiguration(storageServer2, "localhost", "testService")
                    , new NetReceiverConfiguration(1, "fake", "fake"),
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Writer),
                    connection, new RestoreModuleConfiguration(10, new TimeSpan()),
                    new RestoreModuleConfiguration(10, new TimeSpan()),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout),
                    new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)));

                storage1.Build();
                storage2.Build();
                distr.Build();

                storage1.DbModule.AddDbModule(new TestDbInMemory());
                storage2.DbModule.AddDbModule(new TestDbInMemory());

                storage1.Start();
                storage2.Start();
                _proxy.Start();
                distr.Start();

                GlobalQueue.Queue.Start();

                _proxy.Distributor.SayIAmHere(server);

                var api = _proxy.CreateApi("Int", false, new IntHashConvertor());
                UserTransaction transaction;
                var read = api.Read(10, out transaction);

                Assert.Null(read);

                _proxy.Dispose();
                distr.Dispose();
                storage1.Dispose();
                storage2.Dispose();
            }
        }

        [Fact]
        public void ProxyAndDistributor_Read_DirectReadFromTwoServer_TwoReplics_LongRead()
        {
            const int storageServer1 = 22281;
            const int storageServer2 = 22282;            
            const int distrServerForProxy = 24283;
            const int distrServerForDb = 22283;

            var filename = nameof(ProxyAndDistributor_Read_DirectReadFromTwoServer_TwoReplics_LongRead);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                var writer =
                    new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 2, 3,
                        HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                var dhc = new DistributorHashConfiguration(2);
                var queue = new QueueConfiguration(1, 100);
                var connection = new ConnectionConfiguration("testService", 10);
                var dcc = new DistributorCacheConfiguration(TimeSpan.FromMilliseconds(600), TimeSpan.FromMilliseconds(1000));
                var ndrc = new NetReceiverConfiguration(distrServerForDb, "localhost", "testService");
                var ndrc12 = new NetReceiverConfiguration(distrServerForProxy, "localhost", "testService");

                var distr = new DistributorSystem(new ServerId("localhost", distrServerForDb),
                    new ServerId("localhost", distrServerForProxy),
                    dhc, queue, connection, dcc, ndrc, ndrc12,
                    new TransactionConfiguration(1),
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 2, HashFileType.Distributor),
                    new AsyncTasksConfiguration(TimeSpan.FromSeconds(2)),
                    new AsyncTasksConfiguration(TimeSpan.FromSeconds(2)),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));

                var server = new ServerId("localhost", distrServerForProxy);

                var storage1 = new WriterSystem(new ServerId("localhost", storageServer1), queue,
                    new NetReceiverConfiguration(storageServer1, "localhost", "testService")
                    , new NetReceiverConfiguration(1, "fake", "fake"),
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Writer),
                    connection, new RestoreModuleConfiguration(10, new TimeSpan()),
                    new RestoreModuleConfiguration(10, new TimeSpan()),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout),
                    new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)));

                var storage2 = new WriterSystem(new ServerId("localhost", storageServer2), queue,
                    new NetReceiverConfiguration(storageServer2, "localhost", "testService")
                    , new NetReceiverConfiguration(1, "fake", "fake"),
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Writer),
                    connection, new RestoreModuleConfiguration(10, new TimeSpan()),
                    new RestoreModuleConfiguration(10, new TimeSpan()),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout),
                    new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)));

                storage1.Build();
                storage2.Build();
                distr.Build();

                storage1.DbModule.AddDbModule(new TestDbInMemory());
                storage2.DbModule.AddDbModule(new TestDbInMemory());

                _proxy.Start();
                distr.Start();

                _proxy.Distributor.SayIAmHere(server);

                var api = _proxy.CreateApi("Int", false, new IntHashConvertor());

                var task = api.CreateSync(10, 10);
                task.Wait();

                storage1.Start();
                storage2.Start();

                Thread.Sleep(TimeSpan.FromMilliseconds(4000));

                task = api.CreateSync(10, 10);
                task.Wait();
                Assert.Equal(TransactionState.Complete, task.Result.State);

                UserTransaction transaction;

                var data = api.Read(10, out transaction);

                Assert.Equal(10, data);

                _proxy.Dispose();
                distr.Dispose();
                storage1.Dispose();
                storage2.Dispose();
            }
        }
    }
}

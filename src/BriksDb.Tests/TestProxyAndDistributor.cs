using System;
using System.Threading;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestProxy;
using Qoollo.Tests.TestWriter;
using Xunit;

namespace Qoollo.Tests
{
    [Collection("test collection 1")]
    public class TestProxyAndDistributor:TestBase
    {
        private readonly TestProxySystem _proxySystem;

        public TestProxyAndDistributor():base()
        {
            _proxySystem = TestProxySystem(proxyServer);
            _proxySystem.Build();
        }

        [Fact]
        public void ProxyAndDistributor_Create_WriterMock()
        {
            var filename = nameof(ProxyAndDistributor_Create_WriterMock);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                CreateHashFile(filename, 1);

                var distr = DistributorSystem(DistributorCacheConfiguration(600, 1000),
                    filename, 1, distrServer1, distrServer12);

                try
                {
                    distr.Build();
                    _proxySystem.Start();
                    distr.Start();

                    GlobalQueue.Queue.Start();

                    _proxySystem.Distributor.SayIAmHere(ServerId(distrServer1));

                    var api = _proxySystem.CreateApi("", false, new StoredDataHashCalculator());

                    var transaction = api.Create(10, TestHelper.CreateStoredData(10));
                    Assert.NotNull(transaction);
                    Thread.Sleep(200);
                    transaction = _proxySystem.GetTransaction(transaction);
                    Assert.NotNull(transaction);
                    Thread.Sleep(4000);
                    transaction = _proxySystem.GetTransaction(transaction);
                    Assert.NotNull(transaction);
                    Assert.Equal(TransactionState.DontExist, transaction.State);

                    var s = TestHelper.OpenWriterHost(storageServer1);

                    Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                    transaction = api.Create(11, TestHelper.CreateStoredData(11));
                    Assert.NotNull(transaction);
                    Thread.Sleep(200);
                    transaction = _proxySystem.GetTransaction(transaction);
                    GlobalQueue.Queue.TransactionQueue.Add(new Transaction(transaction));
                    Thread.Sleep(100);
                    transaction = _proxySystem.GetTransaction(transaction);
                    Assert.NotNull(transaction);
                    if (transaction.State == TransactionState.TransactionInProcess)
                    {
                        Thread.Sleep(100);
                        transaction = _proxySystem.GetTransaction(transaction);
                    }
                    Assert.Equal(TransactionState.Complete, transaction.State);
                    Thread.Sleep(1000);
                    transaction = _proxySystem.GetTransaction(transaction);
                    Assert.NotNull(transaction);
                    Assert.Equal(TransactionState.DontExist, transaction.State);
                    s.Dispose();
                }
                finally
                {
                    _proxySystem.Dispose();
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
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                writer = new HashWriter(new HashMapConfiguration(filename2, HashMapCreationMode.CreateNew, 2, 3, HashFileType.Writer));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer3, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                var distr = DistributorSystem(DistributorCacheConfiguration(400, 1000), filename1, 2, 
                    distrServer1, distrServer12, 30000, 30000);

                var distr2 = DistributorSystem(DistributorCacheConfiguration(400, 1000), filename2, 2,
                    distrServer2, distrServer22, 30000, 30000);

                try
                {
                    distr.Build();
                    distr2.Build();

                    _proxySystem.Start();
                    distr.Start();
                    distr2.Start();

                    _proxySystem.Distributor.SayIAmHere(ServerId(distrServer1));
                    _proxySystem.Distributor.SayIAmHere(ServerId(distrServer2));

                    var s1 = TestHelper.OpenWriterHost(storageServer1);
                    var s2 = TestHelper.OpenWriterHost(storageServer2);
                    var s3 = TestHelper.OpenWriterHost(storageServer3);

                    Thread.Sleep(TimeSpan.FromMilliseconds(300));

                    var api = _proxySystem.CreateApi("", false, new StoredDataHashCalculator());

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
                    _proxySystem.Dispose();
                    distr.Dispose();
                    distr2.Dispose();
                }
            }
        }

        [Fact]
        public void ProxyAndDistributor_Read_DirectReadFromOneServerMock()
        {
            var filename = nameof(ProxyAndDistributor_Read_DirectReadFromOneServerMock);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                CreateHashFile(filename, 1);

                var distr = DistributorSystem(DistributorCacheConfiguration(600, 1000), filename, 1, 
                    distrServer1, distrServer12, 30000);

                distr.Build();

                _proxySystem.Start();
                distr.Start();

                GlobalQueue.Queue.Start();

                _proxySystem.Distributor.SayIAmHere(ServerId(distrServer1));

                var s = TestHelper.OpenWriterHost(storageServer1);

                s.retData = TestHelper.CreateEvent(new StoredDataHashCalculator(), 10);

                var api = _proxySystem.CreateApi("Event", false, new StoredDataHashCalculator());

                UserTransaction transaction;
                var read = (StoredData)api.Read(10, out transaction);

                Assert.Equal(10, read.Id);
                _proxySystem.Dispose();
                distr.Dispose();
                s.Dispose();
            }
        }

        [Theory]
        [InlineData(50)]
        [InlineData(500)]
        public void ProxyAndDistributor_Read_DirectReadFromOneServer(int count)
        {
            var filename = nameof(ProxyAndDistributor_Read_DirectReadFromOneServer);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                CreateHashFile(filename, 1);

                var distr = DistributorSystem(DistributorCacheConfiguration(600, 1000), filename, 1, 
                    distrServer1, distrServer12, 30000);

                var storage = WriterSystem(filename, 1, storageServer1);
                storage.Build();
                distr.Build();

                storage.DbModule.AddDbModule(new TestDbInMemory());

                storage.Start();
                _proxySystem.Start();
                distr.Start();

                GlobalQueue.Queue.Start();

                _proxySystem.Distributor.SayIAmHere(ServerId(distrServer1));

                var api = _proxySystem.CreateApi("Int", false, new IntHashConvertor());

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

                _proxySystem.Dispose();
                distr.Dispose();
                storage.Dispose();
            }
        }

        [Theory]
        [InlineData(50)]
        [InlineData(500)]
        public void ProxyAndDistributor_Read_DirectReadFromTwoServer(int count)
        {
            var filename = nameof(ProxyAndDistributor_Read_DirectReadFromTwoServer);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                var writer = new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 2, 3, HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                var distr = DistributorSystem(DistributorCacheConfiguration(600, 1000), filename, 1,
                    distrServer1, distrServer12, 30000);

                var storage1 = WriterSystem(filename, 1, storageServer1);
                var storage2 = WriterSystem(filename, 1, storageServer2);

                storage1.Build();
                storage2.Build();
                distr.Build();

                storage1.DbModule.AddDbModule(new TestDbInMemory());
                storage2.DbModule.AddDbModule(new TestDbInMemory());

                storage1.Start();
                storage2.Start();
                _proxySystem.Start();
                distr.Start();

                GlobalQueue.Queue.Start();

                _proxySystem.Distributor.SayIAmHere(ServerId(distrServer1));

                var api = _proxySystem.CreateApi("Int", false, new IntHashConvertor());

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

                _proxySystem.Dispose();
                distr.Dispose();
                storage1.Dispose();
                storage2.Dispose();
            }
        }

        [Theory]
        [InlineData(50)]
        [InlineData(500)]
        public void ProxyAndDistributor_Read_DirectReadFromTwoServer_TwoReplics(int count)
        {
            var filename = nameof(ProxyAndDistributor_Read_DirectReadFromTwoServer_TwoReplics);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                CreateHashFile(filename, 2);

                var distr = DistributorSystem(DistributorCacheConfiguration(600000, 10000000), filename, 2,
                    distrServer1, distrServer12, 30000);

                var storage1 = WriterSystem(filename, 2, storageServer1);
                var storage2 = WriterSystem(filename, 2, storageServer2);

                storage1.Build();
                storage2.Build();
                distr.Build();

                storage1.DbModule.AddDbModule(new TestDbInMemory());
                storage2.DbModule.AddDbModule(new TestDbInMemory());

                storage1.Start();
                storage2.Start();
                _proxySystem.Start();
                distr.Start();

                GlobalQueue.Queue.Start();

                _proxySystem.Distributor.SayIAmHere(ServerId(distrServer1));

                Thread.Sleep(100);
                var api = _proxySystem.CreateApi("Int", false, new IntHashConvertor());

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

                _proxySystem.Dispose();
                distr.Dispose();
                storage1.Dispose();
                storage2.Dispose();
            }
        }

        [Fact]
        public void ProxyAndDistributor_Read_DirectReadFromTwoServer_TwoReplics_NoData()
        {
            var filename = nameof(ProxyAndDistributor_Read_DirectReadFromTwoServer_TwoReplics_NoData);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                CreateHashFile(filename, 2);

                var distr = DistributorSystem(DistributorCacheConfiguration(600, 1000), filename, 2,
                    distrServer1, distrServer12, 30000);

                var storage1 = WriterSystem(filename, 2, storageServer1);
                var storage2 = WriterSystem(filename, 2, storageServer2);

                storage1.Build();
                storage2.Build();
                distr.Build();

                storage1.DbModule.AddDbModule(new TestDbInMemory());
                storage2.DbModule.AddDbModule(new TestDbInMemory());

                storage1.Start();
                storage2.Start();
                _proxySystem.Start();
                distr.Start();

                GlobalQueue.Queue.Start();

                _proxySystem.Distributor.SayIAmHere(ServerId(distrServer1));

                var api = _proxySystem.CreateApi("Int", false, new IntHashConvertor());
                UserTransaction transaction;
                var read = api.Read(10, out transaction);

                Assert.Null(read);

                _proxySystem.Dispose();
                distr.Dispose();
                storage1.Dispose();
                storage2.Dispose();
            }
        }

        [Fact]
        public void ProxyAndDistributor_Read_DirectReadFromTwoServer_TwoReplics_LongRead()
        {
            var filename = nameof(ProxyAndDistributor_Read_DirectReadFromTwoServer_TwoReplics_LongRead);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                CreateHashFile(filename, 2);

                var distr = DistributorSystem(DistributorCacheConfiguration(600, 1000), filename, 2,
                    distrServer1, distrServer12, 120, 120);

                var storage1 = WriterSystem(filename, 2, storageServer1);
                var storage2 = WriterSystem(filename, 2, storageServer2);

                storage1.Build();
                storage2.Build();
                distr.Build();

                storage1.DbModule.AddDbModule(new TestDbInMemory());
                storage2.DbModule.AddDbModule(new TestDbInMemory());

                _proxySystem.Start();
                distr.Start();

                _proxySystem.Distributor.SayIAmHere(ServerId(distrServer1));

                var api = _proxySystem.CreateApi("Int", false, new IntHashConvertor());

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

                _proxySystem.Dispose();
                distr.Dispose();
                storage1.Dispose();
                storage2.Dispose();
            }
        }
    }
}

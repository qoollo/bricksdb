using System;
using System.Threading;
using Ninject;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Modules.Queue;
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
        private readonly TestProxySystem _proxySystem;

        public TestProxyAndDistributor():base()
        {
            _proxySystem = TestProxySystem();
            _proxySystem.Build(new TestInjectionModule());
        }

        [Fact]
        public void Create_WriterMock()
        {
            var filename = nameof(Create_WriterMock);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                CreateHashFile(filename, 1);
                CreateConfigFile(countReplics: 1, hash: filename, timeAliveBeforeDeleteMls: 600,
                    timeAliveAfterUpdateMls: 1000);

                var distr = DistributorSystem();

                try
                {
                    distr.Build(new TestInjectionModule());
                    _proxySystem.Start();
                    distr.Start();

                    _proxySystem.Distributor.SayIAmHere(ServerId(distrServer12));

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

                    var s = TestHelper.OpenWriterHost(_kernel, storageServer1);

                    Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                    transaction = api.Create(11, TestHelper.CreateStoredData(11));
                    Assert.NotNull(transaction);
                    Thread.Sleep(200);
                    transaction = _proxySystem.GetTransaction(transaction);

                    var queue = distr.Kernel.Get<IGlobalQueue>();
                    queue.TransactionQueue.Add(new Transaction(transaction));

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
        public void Create_WriterMock_TwoReplics()
        {
            var filename1 = nameof(Create_WriterMock_TwoReplics) +"1";
            var filename2 = nameof(Create_WriterMock_TwoReplics) +"2";
            using (new FileCleaner(filename1))
            using (new FileCleaner(filename2))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                var writer = new HashWriter(null, filename1, 2);
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                writer = new HashWriter(null, filename2, 2);
                writer.SetServer(0, "localhost", storageServer3, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                CreateConfigFile(hash: filename1, filename: config_file1, 
                    timeAliveBeforeDeleteMls: 400, timeAliveAfterUpdateMls: 1000, ping: 30000, 
                    check: 3000);
                CreateConfigFile(hash: filename2, filename: config_file2,
                    proxyport: distrServer22, writerport:distrServer2,
                    timeAliveBeforeDeleteMls: 400, timeAliveAfterUpdateMls: 1000, ping: 30000, 
                    check: 30000);

                var distr = DistributorSystem();
                var distr2 = DistributorSystem();

                try
                {
                    distr.Build(new TestInjectionModule(), config_file1);
                    distr2.Build(new TestInjectionModule(), config_file2);

                    _proxySystem.Start();
                    distr.Start();
                    distr2.Start();

                    _proxySystem.Distributor.SayIAmHere(ServerId(distrServer12));
                    _proxySystem.Distributor.SayIAmHere(ServerId(distrServer22));

                    var s1 = TestHelper.OpenWriterHost(_kernel, storageServer1);
                    var s2 = TestHelper.OpenWriterHost(_kernel, storageServer2);
                    var s3 = TestHelper.OpenWriterHost(_kernel, storageServer3);

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
        public void Read_DirectReadFromOneServerMock()
        {
            var filename = nameof(Read_DirectReadFromOneServerMock);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                CreateHashFile(filename, 1);
                CreateConfigFile(countReplics: 1, hash: filename,
                    timeAliveBeforeDeleteMls: 600, timeAliveAfterUpdateMls: 1000, check: 30000);

                var distr = DistributorSystem();

                distr.Build(new TestInjectionModule());

                _proxySystem.Start();
                distr.Start();

                _proxySystem.Distributor.SayIAmHere(ServerId(distrServer12));

                var s = TestHelper.OpenWriterHost(_kernel, storageServer1);

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
        public void Read_DirectReadFromOneServer(int count)
        {
            var filename = nameof(Read_DirectReadFromOneServer);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                CreateHashFile(filename, 1);
                CreateConfigFile(countReplics: 1, hash: filename,
                    timeAliveBeforeDeleteMls: 600, timeAliveAfterUpdateMls: 1000, check: 30000);

                var distr = DistributorSystem();

                var storage = WriterSystem();
                storage.Build(new TestInjectionModule());
                distr.Build(new TestInjectionModule());

                storage.DbModule.AddDbModule(new TestDbInMemory());

                storage.Start();
                _proxySystem.Start();
                distr.Start();

                _proxySystem.Distributor.SayIAmHere(ServerId(distrServer12));

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
        public void Read_DirectReadFromTwoServer(int count)
        {
            var filename = nameof(Read_DirectReadFromTwoServer);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(countReplics: 1, hash: filename);
                CreateConfigFile(countReplics: 1, hash: filename, filename: config_file2,
                    distrport: storageServer2, timeAliveBeforeDeleteMls: 600, timeAliveAfterUpdateMls: 1000, 
                    check: 3000);

                var distr = DistributorSystem();

                var storage1 = WriterSystem();
                var storage2 = WriterSystem();

                storage1.Build(new TestInjectionModule());
                storage2.Build(new TestInjectionModule(), config_file2);
                distr.Build(new TestInjectionModule());

                storage1.DbModule.AddDbModule(new TestDbInMemory());
                storage2.DbModule.AddDbModule(new TestDbInMemory());

                storage1.Start();
                storage2.Start();
                _proxySystem.Start();
                distr.Start();

                _proxySystem.Distributor.SayIAmHere(ServerId(distrServer12));

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
        public void Read_DirectReadFromTwoServer_TwoReplics(int count)
        {
            var filename = nameof(Read_DirectReadFromTwoServer_TwoReplics);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(hash: filename);
                CreateConfigFile(hash: filename, filename: config_file2, distrport: storageServer2, 
                    check: 3000);

                var distr = DistributorSystem();

                var storage1 = WriterSystem();
                var storage2 = WriterSystem();

                storage1.Build(new TestInjectionModule());
                storage2.Build(new TestInjectionModule(), config_file2);
                distr.Build(new TestInjectionModule());

                storage1.DbModule.AddDbModule(new TestDbInMemory());
                storage2.DbModule.AddDbModule(new TestDbInMemory());

                storage1.Start();
                storage2.Start();
                _proxySystem.Start();
                distr.Start();

                _proxySystem.Distributor.SayIAmHere(ServerId(distrServer12));

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
        public void Read_DirectReadFromTwoServer_TwoReplics_NoData()
        {
            var filename = nameof(Read_DirectReadFromTwoServer_TwoReplics_NoData);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(hash: filename);
                CreateConfigFile(hash: filename, filename: config_file2, distrport: storageServer2,
                    timeAliveBeforeDeleteMls: 600, timeAliveAfterUpdateMls: 1000, check: 30000);

                var distr = DistributorSystem();

                var storage1 = WriterSystem();
                var storage2 = WriterSystem();

                storage1.Build(new TestInjectionModule());
                storage2.Build(new TestInjectionModule(), config_file2);
                distr.Build(new TestInjectionModule());

                storage1.DbModule.AddDbModule(new TestDbInMemory());
                storage2.DbModule.AddDbModule(new TestDbInMemory());

                storage1.Start();
                storage2.Start();
                _proxySystem.Start();
                distr.Start();

                _proxySystem.Distributor.SayIAmHere(ServerId(distrServer12));

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
        public void Read_DirectReadFromTwoServer_TwoReplics_LongRead()
        {
            var filename = nameof(Read_DirectReadFromTwoServer_TwoReplics_LongRead);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(hash: filename);
                CreateConfigFile(hash: filename, filename: config_file2, distrport: storageServer2,
                    timeAliveBeforeDeleteMls: 600, timeAliveAfterUpdateMls: 1000);

                var distr = DistributorSystem();

                var storage1 = WriterSystem();
                var storage2 = WriterSystem();

                storage1.Build(new TestInjectionModule());
                storage2.Build(new TestInjectionModule(), config_file2);
                distr.Build(new TestInjectionModule());

                storage1.DbModule.AddDbModule(new TestDbInMemory());
                storage2.DbModule.AddDbModule(new TestDbInMemory());

                _proxySystem.Start();
                distr.Start();

                _proxySystem.Distributor.SayIAmHere(ServerId(distrServer12));

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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Client.Configuration;
using Qoollo.Client.DistributorGate;
using Qoollo.Client.Request;
using Qoollo.Client.Support;
using Qoollo.Client.WriterGate;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestModules;
using Qoollo.Tests.TestProxy;
using Qoollo.Tests.TestWriter;
using Consts = Qoollo.Impl.Common.Support.Consts;

namespace Qoollo.Tests
{
    [TestClass]
    public class TestRestore
    {
        private TestWriterGate _writer1;
        private TestWriterGate _writer2;
        private TestWriterGate _writer3;
        private DistributorApi _distr;
        private TestGate _proxy;
        private TestDistributorGate _distrTest;
        const int distrServer1 = 22323;
        const int proxyServer = 22331;
        const int distrServer12 = 22324;
        const int storageServer1 = 22357;
        const int storageServer2 = 22156;
        const int storageServer3 = 22157;

        [TestInitialize]
        public void Initialize()
        {            
            var common = new CommonConfiguration(1, 100);
            var distrNet = new DistributorNetConfiguration("localhost",
                distrServer1, distrServer12, "testService", 10);
            var distrConf = new DistributorConfiguration(1, "TestRestore",
                TimeSpan.FromMilliseconds(10000000), TimeSpan.FromMilliseconds(500000), TimeSpan.FromMinutes(100),
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

        [TestMethod]
        public void Writer_Restore_TwoServers()
        {                        
            var writer =
                new HashWriter(new HashMapConfiguration("test8", HashMapCreationMode.CreateNew, 2, 3,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.SetServer(1, "localhost", storageServer2, 157);
            writer.Save();

            _distrTest.Build(1, distrServer1, distrServer12, "test8");

            _writer1.Build(storageServer1, "test8", 1);
            _writer2.Build(storageServer2, "test8", 1);
            
            _distrTest.Start();
            _writer1.Start();

            var list = new List<InnerData>();
            const int count = 1;
            for (int i = 1; i < count + 1; i++)
            {
                var ev =
                    new InnerData(new Transaction(HashConvertor.GetString(i.ToString(CultureInfo.InvariantCulture)), "")
                    {
                        OperationName = OperationName.Create,
                        OperationType = OperationType.Async
                    })
                    {
                        Data = CommonDataSerializer.Serialize(i),
                        Key = CommonDataSerializer.Serialize(i),
                        Transaction = { Distributor = new ServerId("localhost", distrServer1) }
                    };
                ev.Transaction.TableName = "Int";

                list.Add(ev);
            }

            foreach (var data in list)
            {
                _distrTest.Input.ProcessAsync(data);
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(6000));

            foreach (var data in list)
            {
                var tr = _distrTest.Main.GetTransactionState(data.Transaction.UserTransaction);
                if (tr.State != TransactionState.Complete)
                {
                    data.Transaction = new Transaction(data.Transaction);
                    data.Transaction.ClearError();
                    _distrTest.Input.ProcessAsync(data);
                }
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(5000));

            var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
            var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;

            if (count > 1)
            {
                Assert.AreNotEqual(count, mem.Local);
                Assert.AreNotEqual(count, mem.Remote);
            }
            Assert.AreEqual(count, mem.Local + mem.Remote);

            _writer2.Start();

            _writer2.Distributor.Restore(new ServerId("localhost", distrServer1), false);

            Thread.Sleep(TimeSpan.FromMilliseconds(2000));

            Assert.AreEqual(0, mem.Remote);
            Assert.AreEqual(0, mem2.Remote);
            Assert.AreEqual(count, mem.Local + mem2.Local);
            Assert.AreEqual(false, _writer1.Restore.IsNeedRestore);
            Assert.AreEqual(false, _writer2.Restore.IsNeedRestore);

            _distrTest.Dispose();
            _writer1.Dispose();
            _writer2.Dispose();
        }

        [TestMethod]
        public void Writer_Restore_ThreeServers()
        {                        
            var writer =
                new HashWriter(new HashMapConfiguration("test11", HashMapCreationMode.CreateNew, 3, 3,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.SetServer(1, "localhost", storageServer2, 157);
            writer.SetServer(2, "localhost", storageServer3, 157);
            writer.Save();

            #region hell

            var queue = new QueueConfiguration(2, 100);
            var connection = new ConnectionConfiguration("testService", 10);
            var ndrc2 = new NetReceiverConfiguration(proxyServer, "localhost", "testService");
            var pcc = new ProxyCacheConfiguration(TimeSpan.FromSeconds(20));
            var pccc2 = new ProxyCacheConfiguration(TimeSpan.FromSeconds(40));

            var proxy = new TestProxySystem(new ServerId("localhost", proxyServer),
                queue, connection, pcc, pccc2, ndrc2,
                new AsyncTasksConfiguration(new TimeSpan()),
                new AsyncTasksConfiguration(new TimeSpan()),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
           
            _distrTest.Build(1, distrServer1, distrServer12, "test11");
            _writer1.Build(storageServer1, "test11", 1);
            _writer2.Build(storageServer2, "test11", 1);
            _writer3.Build(storageServer3, "test11", 1);

            #endregion

            #region hell2

            proxy.Build();
            proxy.Start();

           _distrTest.Start();
            _writer1.Start();

            proxy.Distributor.SayIAmHere(new ServerId("localhost", distrServer12));

            Thread.Sleep(TimeSpan.FromMilliseconds(200));

            int count = 50;
            int counter = 0;

            var api = proxy.CreateApi("Int", false, new IntHashConvertor());

            for (int i = 0; i < count; i++)
            {
                bool flag = false;

                while (!flag && counter < 3)
                {
                    var task = api.CreateSync(i + 1, i + 1);
                    task.Wait();
                    flag = true;
                    if (task.Result.IsError)
                    {
                        counter++;
                        flag = false;
                    }
                }
            }
            Assert.AreEqual(2, counter);

            #endregion

            var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
            var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;
            var mem3 = _writer3.Db.GetDbModules.First() as TestDbInMemory;

            if (count > 1)
            {
                Assert.AreNotEqual(count, mem.Local);
                Assert.AreNotEqual(count, mem.Remote);
            }
            Assert.AreEqual(count, mem.Local + mem.Remote);

            _writer2.Start();
            _writer3.Start();

            _writer2.Distributor.Restore(new ServerId("localhost", distrServer1), false);

            Thread.Sleep(TimeSpan.FromMilliseconds(4000));

            _writer3.Distributor.Restore(new ServerId("localhost", distrServer1), false);

            Thread.Sleep(TimeSpan.FromMilliseconds(3000));

            Assert.AreEqual(0, mem.Remote);
            Assert.AreEqual(0, mem2.Remote);
            Assert.AreEqual(0, mem3.Remote);
            Assert.AreEqual(count, mem.Local + mem2.Local + mem3.Local);
            Assert.AreEqual(false, _writer1.Restore.IsNeedRestore);
            Assert.AreEqual(false, _writer2.Restore.IsNeedRestore);
            Assert.AreEqual(false, _writer3.Restore.IsNeedRestore);

            _distrTest.Dispose();
            _writer1.Dispose();
            _writer2.Dispose();
            _writer3.Dispose();

            proxy.Dispose();
        }

        [TestMethod]
        public void Writer_RestoreAfterUpdateHashFile_ThreeServers()
        {            
            var writer =
                new HashWriter(new HashMapConfiguration("TestRestore3ServersUpdate", HashMapCreationMode.CreateNew, 2, 3,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.SetServer(1, "localhost", storageServer2, 157);
            writer.Save();

            _distrTest.Build(1, distrServer1, distrServer12, "TestRestore3ServersUpdate");
            _writer1.Build(storageServer1, "TestRestore3ServersUpdate", 1);
            _writer2.Build(storageServer2, "TestRestore3ServersUpdate", 1);
            _writer3.Build(storageServer3, "TestRestore3ServersUpdate", 1);

            _distrTest.Start();
            _writer1.Start();
            _writer2.Start();

            #region hell

            var list = new List<InnerData>();
            const int count = 50;
            for (int i = 1; i < count + 1; i++)
            {
                var ev =
                    new InnerData(new Transaction(HashConvertor.GetString(i.ToString(CultureInfo.InvariantCulture)), "")
                    {
                        OperationName = OperationName.Create,
                        OperationType = OperationType.Async
                    })
                    {
                        Data = CommonDataSerializer.Serialize(i),
                        Key = CommonDataSerializer.Serialize(i),
                        Transaction = { Distributor = new ServerId("localhost", distrServer1) }
                    };
                ev.Transaction.TableName = "Int";

                list.Add(ev);
            }

            foreach (var data in list)
            {
                _distrTest.Input.ProcessAsync(data);
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(5000));

            #endregion

            var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
            var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;
            var mem3 = _writer3.Db.GetDbModules.First() as TestDbInMemory;

            if (count > 1)
            {
                Assert.AreNotEqual(count, mem.Local);
                Assert.AreNotEqual(count, mem.Remote);
                Assert.AreNotEqual(count, mem2.Local);
                Assert.AreNotEqual(count, mem2.Remote);
            }
            Assert.AreEqual(count, mem.Local + mem2.Local);
            Assert.AreEqual(0, mem.Remote);
            Assert.AreEqual(0, mem2.Remote);

            writer =
                new HashWriter(new HashMapConfiguration("TestRestore3ServersUpdate", HashMapCreationMode.CreateNew, 3, 3,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.SetServer(1, "localhost", storageServer2, 157);
            writer.SetServer(2, "localhost", storageServer3, 157);
            writer.Save();

            _writer3.Start();

            _distrTest.Distributor.UpdateModel();
            _writer1.Distributor.UpdateModel();
            _writer2.Distributor.UpdateModel();

            _writer3.Distributor.Restore(new ServerId("localhost", distrServer1), true);

            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            Assert.AreEqual(0, mem.Remote);
            Assert.AreEqual(0, mem2.Remote);
            Assert.AreEqual(0, mem3.Remote);
            Assert.AreNotEqual(0, mem3.Local);
            Assert.AreEqual(count, mem.Local + mem2.Local + mem3.Local);
            Assert.AreEqual(false, _writer1.Restore.IsNeedRestore);
            Assert.AreEqual(false, _writer2.Restore.IsNeedRestore);
            Assert.AreEqual(false, _writer3.Restore.IsNeedRestore);

            _writer1.Dispose();
            _writer2.Dispose();
            _writer3.Dispose();

            _distrTest.Dispose();
        }

        [TestMethod]
        public void Writer_Restore_TwoServersWhenOneServerNotAvailable()
        {                        
            #region hell

            var writer =
                new HashWriter(new HashMapConfiguration("TestRestore", HashMapCreationMode.CreateNew, 2, 1,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.SetServer(1, "localhost", storageServer2, 157);
            writer.Save();

            var common = new CommonConfiguration(1, 100);            

            var storageNet1 = new StorageNetConfiguration("localhost", storageServer1, 157, "testService", 10);
            var storageConfig = new StorageConfiguration("TestRestore", 1, 10,
                TimeSpan.FromMilliseconds(10000),
                TimeSpan.FromMilliseconds(200), TimeSpan.FromHours(1), TimeSpan.FromHours(1), false);

            var storage1 = new WriterApi(storageNet1, storageConfig, common);
            var storageNet2 = new StorageNetConfiguration("localhost", storageServer2, 157, "testService", 10);
            var storage2 = new WriterApi(storageNet2, storageConfig, common);

            #endregion
            
            _proxy.Start();
            _distr.Start();

            storage1.Build();
            storage1.AddDbModule(new TestInMemoryDbFactory());
            storage1.Start();

            storage1.Api.Restore(new ServerAddress("localhost", distrServer12), false);

            Thread.Sleep(4000);
            Assert.IsTrue(storage1.Api.IsRestoreCompleted());

            var list = storage1.Api.FailedServers();
            Assert.AreEqual(1, list.Count);

            storage2.Build();
            storage2.AddDbModule(new TestInMemoryDbFactory());
            storage2.Start();

            storage1.Api.Restore(new ServerAddress("localhost", distrServer12), list, false);

            Thread.Sleep(1000);
            Assert.IsTrue(storage1.Api.IsRestoreCompleted());

            Thread.Sleep(1000);

            _proxy.Dispose();
            _distr.Dispose();
            storage1.Dispose();
            storage2.Dispose();
        }

        [TestMethod]
        public void Writer_Restore_SelfRestore()
        {
            #region hell

            var writer =
                new HashWriter(new HashMapConfiguration("TestRestore", HashMapCreationMode.CreateNew, 2, 1,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.SetServer(1, "localhost", storageServer2, 157);
            writer.Save();

            var common = new CommonConfiguration(1, 100);

            var storageNet1 = new StorageNetConfiguration("localhost", storageServer1, 157, "testService", 10);
            var storageConfig = new StorageConfiguration("TestRestore", 1, 10, TimeSpan.FromMilliseconds(10000),
                TimeSpan.FromMilliseconds(200), TimeSpan.FromHours(1), TimeSpan.FromHours(1), false);

            var factory = new TestInMemoryDbFactory();
            var storage1 = new WriterApi(storageNet1, storageConfig, common);

            #endregion

            _proxy.Start();
            _distr.Start();

            _proxy.Int.SayIAmHere("localhost", distrServer1);

            storage1.Build();
            storage1.AddDbModule(factory);
            storage1.Start();

            const int count = 50;

            for (int i = 0; i < count; i++)
            {
                var wait = _proxy.Int.CreateSync(i, i);

                if (wait.IsError)
                    wait = _proxy.Int.CreateSync(i, i);

                Assert.AreEqual(RequestState.Complete, wait.State);
            }

            Assert.AreEqual(count, factory.Db.Local + factory.Db.Remote);

            writer =
                new HashWriter(new HashMapConfiguration("TestRestore", HashMapCreationMode.CreateNew, 1, 1,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.Save();

            storage1.Api.UpdateModel();
            storage1.Api.Restore(new ServerAddress("localhost", distrServer12), true);

            Thread.Sleep(1000);

            Assert.AreEqual(count, factory.Db.Local);

            _proxy.Dispose();
            _distr.Dispose();
            storage1.Dispose();
        }

        [TestMethod]
        public void Writer_Restore_TimeoutDelete()
        {                           
            #region hell

            var writer =
                new HashWriter(new HashMapConfiguration("TestRestore", HashMapCreationMode.CreateNew, 1, 1,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.Save();      

            var storageNet1 = new StorageNetConfiguration("localhost", storageServer1, 157, "testService", 10);
            var storageConfig = new StorageConfiguration("TestRestore", 1, 10,
                TimeSpan.FromMilliseconds(10000),
                TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(1), TimeSpan.FromSeconds(1), false);

            var factory = new TestInMemoryDbFactory();
            var storage1 = new WriterApi(storageNet1, storageConfig, new CommonConfiguration(1, 10));

            #endregion
            
            _proxy.Start();
            
            _distr.Start();

            _proxy.Int.SayIAmHere("localhost", distrServer1);

            storage1.Build();
            storage1.AddDbModule(factory);
            storage1.Start();

            const int count = 50;

            for (int i = 0; i < count; i++)
            {
                var wait = _proxy.Int.CreateSync(i, i);

                Assert.AreEqual(RequestState.Complete, wait.State);
            }

            Assert.AreEqual(count, factory.Db.Local);

            for (int i = 0; i < count / 2; i++)
            {
                var wait = _proxy.Int.DeleteSync(i);

                Assert.AreEqual(RequestState.Complete, wait.State);
            }

            Assert.AreEqual(count / 2, factory.Db.Local);
            Assert.AreEqual(count / 2, factory.Db.Deleted);

            Thread.Sleep(4000);

            Assert.AreEqual(count / 2, factory.Db.Local);
            Assert.AreEqual(0, factory.Db.Deleted);

            _proxy.Dispose();
            _distr.Dispose();
            storage1.Dispose();
        }
    }
}

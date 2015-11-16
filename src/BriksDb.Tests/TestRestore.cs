using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Client.Configuration;
using Qoollo.Client.Request;
using Qoollo.Client.Support;
using Qoollo.Client.WriterGate;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.TestSupport;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestProxy;
using Qoollo.Tests.TestWriter;
using Consts = Qoollo.Impl.Common.Support.Consts;

namespace Qoollo.Tests
{
    [TestClass]
    public class TestRestore : TestBase
    {
        private void CreateRestoreFile(string filename, string tableName, RestoreState state,
             List<RestoreServerSave> servers = null)
        {
            using (var writer = new StreamWriter(filename))
            {
                writer.WriteLine("<?xml version=\"1.0\"?>");
                writer.WriteLine(
                    "<RestoreSaveHelper xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"  TableName=\"{0}\" RestoreState=\"{1}\">",
                    string.IsNullOrEmpty(tableName) ? "AllTablesyNameThatMustntBeUsedAsTableName" : tableName,
                    Enum.GetName(typeof (RestoreState), state));

                if(servers != null)
                {
                    writer.WriteLine("<RestoreServers>");
                    foreach (var server in servers)
                    {
                        writer.WriteLine("<RestoreServerSave>");
                        writer.WriteLine("<IsNeedRestore>{0}</IsNeedRestore>", server.IsNeedRestore.ToString().ToLower());
                        writer.WriteLine("<IsRestored>{0}</IsRestored>", server.IsRestored.ToString().ToLower());
                        writer.WriteLine("<IsFailed>{0}</IsFailed>", server.IsFailed.ToString().ToLower());
                        writer.WriteLine("<Port>{0}</Port>", server.Port);
                        writer.WriteLine("<Host>{0}</Host>", server.Host);
                        writer.WriteLine("</RestoreServerSave>");
                    }
                    writer.WriteLine("</RestoreServers>");
                }
                writer.WriteLine("</RestoreSaveHelper>");                
            }
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
            const int count = 50;
            for (int i = 1; i < count + 1; i++)
            {
                var ev =
                    new InnerData(new Transaction(HashConvertor.GetString(i.ToString(CultureInfo.InvariantCulture)),
                        "default")
                    {
                        OperationName = OperationName.Create,
                        OperationType = OperationType.Async
                    })
                    {
                        Data = CommonDataSerializer.Serialize(i),
                        Key = CommonDataSerializer.Serialize(i),
                        Transaction = {Distributor = new ServerId("localhost", distrServer1)}
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
            _writer2.Distributor.Restore(false);

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

            const int count = 50;
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

            _writer2.Distributor.Restore(false);

            Thread.Sleep(TimeSpan.FromMilliseconds(4000));

            _writer3.Distributor.Restore(false);

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
            const string fileName = "TestRestore3ServersUpdate";
            var func = new Action<string>(file =>
            {
                var writer = new HashWriter(new HashMapConfiguration(file, HashMapCreationMode.CreateNew, 2, 3,
                    HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();
            });

            var func2 = new Action<string>(file =>
            {
                var writer = new HashWriter(new HashMapConfiguration(file, HashMapCreationMode.CreateNew, 3, 3,
                    HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.SetServer(2, "localhost", storageServer3, 157);
                writer.Save();
            });

            func(fileName);
            func("1" + fileName);
            func("2" + fileName);
            func2("3" + fileName);

            _distrTest.Build(1, distrServer1, distrServer12, fileName);
            _writer1.Build(storageServer1, "1" + fileName, 1);
            _writer2.Build(storageServer2, "2" + fileName, 1);
            _writer3.Build(storageServer3, "3" + fileName, 1);

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
                        Transaction = {Distributor = new ServerId("localhost", distrServer1)}
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

            func2(fileName);

            _writer3.Start();

            _distrTest.Distributor.UpdateModel();
            _writer1.Distributor.UpdateModel();
            _writer2.Distributor.UpdateModel();

            _writer3.Distributor.Restore(true);

            Thread.Sleep(TimeSpan.FromMilliseconds(1400));

            Assert.AreEqual(0, mem.Remote);
            Assert.AreEqual(0, mem2.Remote);
            Assert.AreEqual(0, mem3.Remote);
            Assert.AreNotEqual(0, mem3.Local);
            Assert.AreEqual(count, mem.Local + mem2.Local + mem3.Local);
            Assert.AreEqual(true, _writer1.Restore.IsNeedRestore);
            Assert.AreEqual(true, _writer2.Restore.IsNeedRestore);
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

            storage1.Api.Restore(false);

            Thread.Sleep(4000);
            Assert.IsFalse(storage1.Api.IsRestoreCompleted());

            var list = storage1.Api.FailedServers();
            Assert.AreEqual(1, list.Count);

            storage2.Build();
            storage2.AddDbModule(new TestInMemoryDbFactory());
            storage2.Start();            

            Thread.Sleep(2000);
            Assert.IsTrue(storage1.Api.IsRestoreCompleted());
            
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
            storage1.Api.Restore(true);

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

            for (int i = 0; i < count/2; i++)
            {
                var wait = _proxy.Int.DeleteSync(i);

                Assert.AreEqual(RequestState.Complete, wait.State);
            }

            Assert.AreEqual(count/2, factory.Db.Local);
            Assert.AreEqual(count/2, factory.Db.Deleted);

            Thread.Sleep(4000);

            Assert.AreEqual(count/2, factory.Db.Local);
            Assert.AreEqual(0, factory.Db.Deleted);

            _proxy.Dispose();
            _distr.Dispose();
            storage1.Dispose();
        }

        [TestMethod]
        public void Writer_Restore_ThreeServersTwoReplics()
        {
            var writer =
                new HashWriter(new HashMapConfiguration("Writer_Restore_ThreeServersTwoReplics",
                    HashMapCreationMode.CreateNew, 3, 3,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.SetServer(1, "localhost", storageServer2, 157);
            writer.SetServer(2, "localhost", storageServer3, 157);
            writer.Save();

            _proxy.Start();
            _distrTest.Build(2, distrServer1, distrServer12, "Writer_Restore_ThreeServersTwoReplics");
            _writer1.Build(storageServer1, "Writer_Restore_ThreeServersTwoReplics", 2);
            _writer2.Build(storageServer2, "Writer_Restore_ThreeServersTwoReplics", 2);
            _writer3.Build(storageServer3, "Writer_Restore_ThreeServersTwoReplics", 2);

            _distrTest.Start();
            _writer1.Start();
            _writer2.Start();

            _proxy.Int.SayIAmHere("localhost", distrServer12);

            Thread.Sleep(TimeSpan.FromMilliseconds(200));

            const int count = 50;


            for (int i = 0; i < count; i++)
            {
                if (_proxy.Int.CreateSync(i, i).IsError)
                    _proxy.Int.CreateSync(i, i);
            }

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
            Assert.AreEqual(count*2, mem.Local + mem.Remote + mem2.Local + mem2.Remote);

            _writer3.Start();

            _writer3.Distributor.Restore(false);

            Thread.Sleep(TimeSpan.FromMilliseconds(3000));

            Assert.AreEqual(0, mem.Remote);
            Assert.AreEqual(0, mem2.Remote);
            Assert.AreEqual(0, mem3.Remote);
            Assert.AreEqual(count*2, mem.Local + mem2.Local + mem3.Local);
            Assert.AreEqual(false, _writer1.Restore.IsNeedRestore);
            Assert.AreEqual(false, _writer2.Restore.IsNeedRestore);
            Assert.AreEqual(false, _writer3.Restore.IsNeedRestore);

            _distrTest.Dispose();
            _writer1.Dispose();
            _writer2.Dispose();
            _writer3.Dispose();

            _proxy.Dispose();
        }

        [TestMethod]
        public void Writer_Restore_ThreeServersTwoReplics_UpdateModel()
        {
            var writer =
                new HashWriter(new HashMapConfiguration("Writer_Restore_ThreeServersTwoReplics_UpdateModel",
                    HashMapCreationMode.CreateNew, 2, 2,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.SetServer(1, "localhost", storageServer2, 157);
            writer.Save();

            _proxy.Start();
            _distrTest.Build(2, distrServer1, distrServer12, "Writer_Restore_ThreeServersTwoReplics_UpdateModel");
            _writer1.Build(storageServer1, "Writer_Restore_ThreeServersTwoReplics_UpdateModel", 2);
            _writer2.Build(storageServer2, "Writer_Restore_ThreeServersTwoReplics_UpdateModel", 2);
            _writer3.Build(storageServer3, "Writer_Restore_ThreeServersTwoReplics_UpdateModel", 2);

            _distrTest.Start();
            _writer1.Start();
            _writer2.Start();

            _proxy.Int.SayIAmHere("localhost", distrServer12);

            Thread.Sleep(TimeSpan.FromMilliseconds(200));

            const int count = 50;


            for (int i = 0; i < count; i++)
            {
                if (_proxy.Int.CreateSync(i, i).IsError)
                    _proxy.Int.CreateSync(i, i);
            }

            var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
            var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;
            var mem3 = _writer3.Db.GetDbModules.First() as TestDbInMemory;

            Assert.AreEqual(count, mem.Local);
            Assert.AreEqual(0, mem.Remote);

            Assert.AreEqual(count, mem2.Local);
            Assert.AreEqual(0, mem2.Remote);

            writer =
                new HashWriter(new HashMapConfiguration("Writer_Restore_ThreeServersTwoReplics_UpdateModel",
                    HashMapCreationMode.CreateNew, 3, 3,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.SetServer(1, "localhost", storageServer2, 157);
            writer.SetServer(2, "localhost", storageServer3, 157);
            writer.Save();

            var localLast = mem.Local;
            var localLast2 = mem2.Local;

            _writer3.Start();

            _writer1.Distributor.UpdateModel();
            _writer2.Distributor.UpdateModel();

            _writer3.Distributor.Restore(true);
            Thread.Sleep(TimeSpan.FromMilliseconds(3000));

            _writer2.Distributor.Restore(true);
            Thread.Sleep(TimeSpan.FromMilliseconds(3000));

            _writer1.Distributor.Restore(true);
            Thread.Sleep(TimeSpan.FromMilliseconds(3000));

            Assert.AreEqual(0, mem.Remote);
            Assert.AreEqual(0, mem2.Remote);
            Assert.AreEqual(0, mem3.Remote);
            Assert.AreEqual(count*2, mem.Local + mem2.Local + mem3.Local);
            Assert.AreEqual(false, _writer1.Restore.IsNeedRestore);
            Assert.AreEqual(false, _writer2.Restore.IsNeedRestore);
            Assert.AreEqual(false, _writer3.Restore.IsNeedRestore);

            Assert.AreNotEqual(localLast, mem.Local);
            Assert.AreNotEqual(localLast2, mem2.Local);

            _distrTest.Dispose();
            _writer1.Dispose();
            _writer2.Dispose();
            _writer3.Dispose();

            _proxy.Dispose();
        }

        [TestMethod]
        public void Distributor_RestoreWithDistributirStateCheck_WithoutModelUpdate()
        {
            const string fileName = "Distributor_Restore";
            var writer =
                new HashWriter(new HashMapConfiguration(fileName, HashMapCreationMode.CreateNew, 2, 3,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.SetServer(1, "localhost", storageServer2, 157);
            writer.Save();

            _writer1.Build(storageServer1, fileName, 1);
            _writer2.Build(storageServer2, fileName, 1);

            _proxy.Start();
            _writer1.Start();

            _distrTest.Build(1, distrServer1, distrServer12, fileName, TimeSpan.FromMilliseconds(1000));
            _distrTest.Start();

            _proxy.Int.SayIAmHere("localhost", distrServer12);

            var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
            var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;

            const int count = 50;
            for (int i = 0; i < count; i++)
            {
                var result = _proxy.Int.CreateSync(i, i);
                if (result.IsError)
                {
                    _proxy.Int.CreateSync(i, i);
                }
            }

            Assert.AreEqual(count, mem.Local + mem.Remote);

            Assert.IsTrue(_distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer1).IsAvailable);
            Assert.IsFalse(_distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer2).IsAvailable);

            Assert.IsTrue(_distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer1).IsServerRestored);
            Assert.IsFalse(_distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer2).IsServerRestored);

            Assert.AreEqual(RestoreState.Restored,
                _distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer1).RestoreState);

            Assert.AreEqual(RestoreState.SimpleRestoreNeed,
                _distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer2).RestoreState);

            _writer2.Start();

            Thread.Sleep(TimeSpan.FromMilliseconds(1200));

            Assert.AreEqual(RestoreState.SimpleRestoreNeed, _writer2.Distributor.GetRestoreRequiredState());

            _writer2.Distributor.Restore(false);

            Thread.Sleep(TimeSpan.FromMilliseconds(2000));

            Assert.AreEqual(RestoreState.Restored, _writer2.Distributor.GetRestoreRequiredState());
            Assert.IsTrue(_distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer2).IsServerRestored);

            Assert.AreEqual(RestoreState.Restored,
                _distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer2).RestoreState);

            Assert.AreEqual(count, mem.Local + mem2.Local);
            
            for (int i = 0; i < count; i++)
            {
                var result = _proxy.Int.CreateSync(i + 50, i);
                if (result.IsError)
                {
                    _proxy.Int.CreateSync(i + 50, i);
                }
            }

            Assert.AreEqual(count*2, mem.Local + mem2.Local);

            _proxy.Dispose();
            _distrTest.Dispose();
            _writer1.Dispose();
            _writer2.Dispose();
        }

        [TestMethod]
        public void Distributor_RestoreWithDistributirStateCheck_WithModelUpdate_RestoreAllServers()
        {
            var func = new Action<string>(file =>
            {
                var writer =
                    new HashWriter(new HashMapConfiguration(file, HashMapCreationMode.CreateNew, 2, 3,
                        HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();
            });

            var func2 = new Action<string>(file =>
            {
                var writer =
                    new HashWriter(new HashMapConfiguration(file, HashMapCreationMode.CreateNew, 3, 3,
                        HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.SetServer(2, "localhost", storageServer3, 157);
                writer.Save();
            });

            const string fileName = "Distributor_Restore";
            const string fileName2 = "Distributor_2Restore";
            const string fileName3 = "Distributor_3Restore";
            const string fileName4 = "Distributor_4Restore";

            func(fileName);
            func(fileName2);
            func(fileName3);
            func2(fileName4);


            _writer1.Build(storageServer1, fileName2, 1);
            _writer2.Build(storageServer2, fileName3, 1);

            _proxy.Start();
            _writer1.Start();
            _writer2.Start();

            _distrTest.Build(1, distrServer1, distrServer12, fileName, TimeSpan.FromMilliseconds(1000));
            _distrTest.Start();

            _proxy.Int.SayIAmHere("localhost", distrServer12);

            var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
            var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;

            const int count = 50;
            for (int i = 0; i < count; i++)
            {
                _proxy.Int.CreateSync(i, i);
            }

            Assert.AreEqual(count, mem.Local + mem2.Local);
            func2(fileName);

            _writer3.Build(storageServer3, fileName4, 1);
            _writer3.Start();
            var mem3 = _writer3.Db.GetDbModules.First() as TestDbInMemory;

            _distrTest.Distributor.UpdateModel();

            Thread.Sleep(TimeSpan.FromMilliseconds(1500));

            Assert.AreEqual(3, _distrTest.WriterSystemModel.Servers.Count);
            Assert.IsTrue(_distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer1).IsAvailable);
            Assert.IsTrue(_distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer2).IsAvailable);
            Assert.IsTrue(_distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer3).IsAvailable);

            Assert.AreEqual(RestoreState.FullRestoreNeed,
                _distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer1).RestoreState);
            Assert.AreEqual(RestoreState.FullRestoreNeed,
                _distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer2).RestoreState);
            Assert.AreEqual(RestoreState.FullRestoreNeed,
                _distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer3).RestoreState);

            Assert.AreEqual(RestoreState.FullRestoreNeed, _writer1.Distributor.GetRestoreRequiredState());
            Assert.AreEqual(RestoreState.FullRestoreNeed, _writer2.Distributor.GetRestoreRequiredState());
            Assert.AreEqual(RestoreState.FullRestoreNeed, _writer3.Distributor.GetRestoreRequiredState());

            _writer1.Distributor.Restore(true);
            Thread.Sleep(TimeSpan.FromMilliseconds(1500));
            _writer2.Distributor.Restore(true);
            Thread.Sleep(TimeSpan.FromMilliseconds(1500));
            _writer3.Distributor.Restore(true);
            Thread.Sleep(TimeSpan.FromMilliseconds(1500));

            Assert.AreEqual(count, mem.Local + mem2.Local + mem3.Local);

            Assert.AreEqual(RestoreState.Restored,
                _distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer1).RestoreState);
            Assert.AreEqual(RestoreState.Restored,
                _distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer2).RestoreState);
            Assert.AreEqual(RestoreState.Restored,
                _distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer3).RestoreState);

            Assert.AreEqual(RestoreState.Restored, _writer1.Distributor.GetRestoreRequiredState());
            Assert.AreEqual(RestoreState.Restored, _writer2.Distributor.GetRestoreRequiredState());
            Assert.AreEqual(RestoreState.Restored, _writer3.Distributor.GetRestoreRequiredState());

            _proxy.Dispose();
            _distrTest.Dispose();
            _writer1.Dispose();
            _writer2.Dispose();
            _writer3.Dispose();
        }

        [TestMethod]
        public void Writer_Restore_TwoServer_RestoreFromFile()
        {
            var writer =
                new HashWriter(new HashMapConfiguration("Writer_Restore_TwoServer_RestoreFromFile", HashMapCreationMode.CreateNew, 2, 3,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.SetServer(1, "localhost", storageServer2, 157);
            writer.Save();

            _distrTest.Build(1, distrServer1, distrServer12, "Writer_Restore_TwoServer_RestoreFromFile");

            const string restoreFile1 = "restore1.txt";
            const string restoreFile2 = "restore2.txt";

            InitInjection.RestoreHelpFileOut = restoreFile1;
            _writer1.Build(storageServer1, "Writer_Restore_TwoServer_RestoreFromFile", 1);
            InitInjection.RestoreHelpFileOut = restoreFile2;
            CreateRestoreFile(restoreFile2, string.Empty, RestoreState.SimpleRestoreNeed,
                new List<RestoreServerSave>
                {
                    new RestoreServerSave(new RestoreServer("localhost", storageServer1)
                    {IsFailed = false, IsRestored = false, IsNeedRestore = true})
                });
            _writer2.Build(storageServer2, "Writer_Restore_TwoServer_RestoreFromFile", 1);

            _distrTest.Start();
            _writer1.Start();

            var list = new List<InnerData>();
            const int count = 50;
            for (int i = 1; i < count + 1; i++)
            {
                var ev =
                    new InnerData(new Transaction(HashConvertor.GetString(i.ToString(CultureInfo.InvariantCulture)),
                        "default")
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

            Thread.Sleep(TimeSpan.FromMilliseconds(4000));

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
        public void Writer_Restore_ThreeServers_RestoreFromFile()
        {
            var writer =
                new HashWriter(new HashMapConfiguration("Writer_Restore_ThreeServers_RestoreFromFile",
                    HashMapCreationMode.CreateNew, 3, 3, HashFileType.Distributor));
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

            const string restoreFile1 = "restore1.txt";
            const string restoreFile2 = "restore2.txt";
            const string restoreFile3 = "restore3.txt";

            _distrTest.Build(1, distrServer1, distrServer12, "Writer_Restore_ThreeServers_RestoreFromFile");

            InitInjection.PingPeriodOut = TimeSpan.FromMilliseconds(1000);

            InitInjection.RestoreHelpFileOut = restoreFile1;
            _writer1.Build(storageServer1, "Writer_Restore_ThreeServers_RestoreFromFile", 1);

            InitInjection.RestoreHelpFileOut = restoreFile2;
            CreateRestoreFile(restoreFile2, string.Empty, RestoreState.SimpleRestoreNeed,
               new List<RestoreServerSave>
                {
                    new RestoreServerSave(new RestoreServer("localhost", storageServer1)
                    {IsFailed = false, IsRestored = false, IsNeedRestore = true}),
                    new RestoreServerSave(new RestoreServer("localhost", storageServer3)
                    {IsFailed = false, IsRestored = false, IsNeedRestore = true})
                });
            _writer2.Build(storageServer2, "Writer_Restore_ThreeServers_RestoreFromFile", 1);

            InitInjection.RestoreHelpFileOut = restoreFile3;
            CreateRestoreFile(restoreFile3, string.Empty, RestoreState.SimpleRestoreNeed,
               new List<RestoreServerSave>
                {
                    new RestoreServerSave(new RestoreServer("localhost", storageServer1)
                    {IsFailed = false, IsRestored = false, IsNeedRestore = true}),
                    new RestoreServerSave(new RestoreServer("localhost", storageServer2)
                    {IsFailed = false, IsRestored = false, IsNeedRestore = true})
                });
            _writer3.Build(storageServer3, "Writer_Restore_ThreeServers_RestoreFromFile", 1);

            #endregion

            #region hell2

            proxy.Build();
            proxy.Start();

            _distrTest.Start();
            _writer1.Start();

            proxy.Distributor.SayIAmHere(new ServerId("localhost", distrServer12));

            Thread.Sleep(TimeSpan.FromMilliseconds(200));

            const int count = 50;
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
            Thread.Sleep(TimeSpan.FromMilliseconds(6000));

            _writer3.Start();
            Thread.Sleep(TimeSpan.FromMilliseconds(5000));

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
        public void Writer_Restore_TwoServer_RestoreFromDistributor()
        {
            var writer =
                new HashWriter(new HashMapConfiguration("Writer_Restore_TwoServer_RestoreFromDistributor", HashMapCreationMode.CreateNew, 2, 3,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.SetServer(1, "localhost", storageServer2, 157);
            writer.Save();

            _distrTest.Build(1, distrServer1, distrServer12, "Writer_Restore_TwoServer_RestoreFromDistributor",
                TimeSpan.FromMilliseconds(1000), true);

            const string restoreFile1 = "restore1.txt";
            const string restoreFile2 = "restore2.txt";

            InitInjection.RestoreHelpFileOut = restoreFile1;
            _writer1.Build(storageServer1, "Writer_Restore_TwoServer_RestoreFromDistributor", 1);
            InitInjection.RestoreHelpFileOut = restoreFile2;
            _writer2.Build(storageServer2, "Writer_Restore_TwoServer_RestoreFromDistributor", 1);

            _distrTest.Start();
            _writer1.Start();

            var list = new List<InnerData>();
            const int count = 50;
            for (int i = 1; i < count + 1; i++)
            {
                var ev =
                    new InnerData(new Transaction(HashConvertor.GetString(i.ToString(CultureInfo.InvariantCulture)),
                        "default")
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
            Assert.AreEqual(0, mem2.Local + mem2.Remote);

            _writer2.Start();

            Thread.Sleep(TimeSpan.FromMilliseconds(5000));

            Assert.AreEqual(0, mem.Remote);
            Assert.AreEqual(0, mem2.Remote);
            Assert.AreEqual(count, mem.Local + mem2.Local);
            Assert.AreEqual(false, _writer1.Restore.IsNeedRestore);
            Assert.AreEqual(false, _writer2.Restore.IsNeedRestore);

            _distrTest.Dispose();
            _writer1.Dispose();
            _writer2.Dispose();
        }
    }
}

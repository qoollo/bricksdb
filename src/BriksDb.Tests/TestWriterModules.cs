using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Client.Support;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Components;
using Qoollo.Impl.Configurations;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestProxy;
using Qoollo.Tests.TestWriter;

namespace Qoollo.Tests
{
    [TestClass]
    public class TestWriterModules
    {
        private TestProxySystem _proxy;
        private TestWriterGate _writer1;
        private TestWriterGate _writer2;
        private TestDistributorGate _distributor1;
        private TestDistributorGate _distributor2;

        [TestInitialize]
        public void Initialize()
        {
            const int proxyServer = 22020;
            var queue = new QueueConfiguration(2, 100);
            var connection = new ConnectionConfiguration("testService", 10);
            var ndrc2 = new NetReceiverConfiguration(proxyServer, "localhost", "testService");
            var pcc = new ProxyCacheConfiguration(TimeSpan.FromSeconds(20));
            var pccc2 = new ProxyCacheConfiguration(TimeSpan.FromSeconds(40));

            _proxy = new TestProxySystem(new ServerId("localhost", proxyServer),
                queue, connection, pcc, pccc2, ndrc2,
                new AsyncTasksConfiguration(new TimeSpan()),
                new AsyncTasksConfiguration(new TimeSpan()),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));

            _proxy.Build();

            _writer1 = new TestWriterGate();
            _writer2 = new TestWriterGate();
            _distributor1 = new TestDistributorGate();
            _distributor2 = new TestDistributorGate();
        }

        [TestMethod]
        public void DbModule_LocalAndRemoteData_Count()
        {
            var provider = new IntHashConvertor();

            var writer =
                new HashWriter(new HashMapConfiguration("TestLocalAndRemote", HashMapCreationMode.CreateNew, 2, 3,
                    HashFileType.Collector));
            writer.CreateMap();
            writer.SetServer(0, "localhost", 157, 157);
            writer.SetServer(1, "localhost", 11011, 157);
            writer.Save();

            _writer1.Build(157, "TestLocalAndRemote", 1);            

            var list = new List<InnerData>();
            const int count = 100;
            for (int i = 0; i < count; i++)
            {
                var ev =
                    new InnerData(new Transaction(provider.CalculateHashFromKey(i), "")
                    {
                        OperationName = OperationName.Create
                    })
                    {
                        Data = CommonDataSerializer.Serialize(i),
                        Key = CommonDataSerializer.Serialize(i),
                        Transaction = { Distributor = new ServerId("localhost", 22188) }
                    };
                ev.Transaction.TableName = "Int";

                list.Add(ev);
            }

            TestHelper.OpenDistributorHostForDb(new ServerId("localhost", 22188), new ConnectionConfiguration("testService", 10));
            
            _writer1.Start();            

            foreach (var data in list)
            {
                _writer1.Input.Process(data);
            }

            Thread.Sleep(1000);

            var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
            Assert.AreNotEqual(count, mem.Local);
            Assert.AreNotEqual(count, mem.Remote);
            Assert.AreEqual(count, mem.Local + mem.Remote);

            _writer1.Dispose();
        }

        [TestMethod]
        public void Writer_SendRestoreCommandToDistributors_RestoreRemoteTable()
        {            
            const int distrServer1 = 22113;
            const int distrServer12 = 23113;
            const int distrServer2 = 22114;
            const int distrServer22 = 23114;
            const int storageServer1 = 22115;
            const int storageServer2 = 22116;

            var writer =
                new HashWriter(new HashMapConfiguration("test7", HashMapCreationMode.CreateNew, 2, 3,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.SetServer(1, "localhost", storageServer2, 157);
            writer.Save();

            writer =
                new HashWriter(new HashMapConfiguration("test6", HashMapCreationMode.CreateNew, 2, 3,
                    HashFileType.Writer));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.SetServer(1, "localhost", storageServer2, 157);
            writer.Save();

            _distributor1.Build(2, distrServer1, distrServer12, "test7");
            _distributor2.Build(2, distrServer2, distrServer22, "test6");

            _writer1.Build(storageServer1, "test6", 2);
            _writer2.Build(storageServer2, "test7", 2);
            
            _proxy.Start();

            _distributor1.Start();
            _distributor2.Start();

            _proxy.Distributor.SayIAmHere(new ServerId("localhost", distrServer12));
            _proxy.Distributor.SayIAmHere(new ServerId("localhost", distrServer22));

            _distributor2.Distributor.SayIAmHereRemoteResult(new ServerId("localhost", distrServer12));

            Thread.Sleep(TimeSpan.FromMilliseconds(300));
            Assert.AreEqual(1, _distributor1.Distributor.GetDistributors().Count);
            Assert.AreEqual(1, _distributor2.Distributor.GetDistributors().Count);

            var api = _proxy.CreateApi("Int", false, new IntHashConvertor());

            var tr1 = api.CreateSync(10, 10);
            var tr2 = api.CreateSync(11, 11);

            tr1.Wait();
            tr2.Wait();

            _writer1.Start();
            _writer2.Start();

            _writer1.Distributor.Restore(new ServerId("localhost", distrServer1), false);

            _writer2.Distributor.Restore(new ServerId("localhost", distrServer2), false);

            Thread.Sleep(TimeSpan.FromMilliseconds(2000));

            var tr3 = api.CreateSync(12, 12);
            var tr4 = api.CreateSync(13, 13);

            tr3.Wait();
            tr4.Wait();

            var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
            var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;

            Assert.AreEqual(2, mem.Local);
            Assert.AreEqual(2, mem2.Local);

            _writer1.Dispose();
            _writer2.Dispose();

            _proxy.Dispose();
            
            _distributor1.Dispose();
            _distributor2.Dispose();
        }

        [TestMethod]
        public void Writer_ProcessData_SendResultToDistributerMock()
        {
            const int distributorServer1 = 22171;
            const int storageServer1 = 22172;

            var writer =
                new HashWriter(new HashMapConfiguration("TestDbTransaction", HashMapCreationMode.CreateNew, 1, 1,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.Save();

            _writer1.Build(storageServer1, "TestDbTransaction", 1);
            _writer1.Start();

            var s = TestHelper.OpenDistributorHostForDb(new ServerId("localhost", distributorServer1),
                new ConnectionConfiguration("testService", 10));

            var list = new List<InnerData>();
            const int count = 100;
            for (int i = 1; i < count + 1; i++)
            {
                var ev =
                    new InnerData(new Transaction(HashConvertor.GetString(i.ToString(CultureInfo.InvariantCulture)), "")
                    {
                        OperationName = OperationName.Create
                    })
                    {
                        Data = CommonDataSerializer.Serialize(i),
                        Key = CommonDataSerializer.Serialize(i),
                        Transaction = { Distributor = new ServerId("localhost", distributorServer1) }
                    };
                ev.Transaction.TableName = "Int";

                list.Add(ev);
            }

            foreach (var data in list)
            {
                _writer1.Q.DbInputProcessQueue.Add(data);
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            Assert.AreEqual(count, s.SendValue);
            _writer1.Dispose();
        }

        [TestMethod]
        public void Writer_ProcessData_SendResultToTwoDistributeMocks()
        {
            const int distributorServer1 = 22173;
            const int distributorServer2 = 22174;
            const int storageServer1 = 22175;
            
            var writer =
                new HashWriter(new HashMapConfiguration("TestDbTransaction2Distributors", HashMapCreationMode.CreateNew,
                    1, 1, HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.Save();

            _writer1.Build(storageServer1, "TestDbTransaction2Distributors", 1);
            _writer1.Start();

            var s = TestHelper.OpenDistributorHostForDb(new ServerId("localhost", distributorServer1),
                new ConnectionConfiguration("testService", 10));

            var s2 = TestHelper.OpenDistributorHostForDb(new ServerId("localhost", distributorServer2),
                new ConnectionConfiguration("testService", 10));

            var list = new List<InnerData>();
            const int count = 100;
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
                        Transaction = { Distributor = new ServerId("localhost", distributorServer1) }
                    };
                ev.Transaction.TableName = "Int";

                list.Add(ev);
            }

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
                        Transaction = { Distributor = new ServerId("localhost", distributorServer2) }
                    };
                ev.Transaction.TableName = "Int";

                list.Add(ev);
            }

            foreach (var data in list)
            {
                _writer1.Q.DbInputProcessQueue.Add(data);
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            Assert.AreEqual(count, s.SendValue);
            Assert.AreEqual(count, s2.SendValue);

            _writer1.Dispose();
        }

        [TestMethod]
        public void Writer_ProcessDataFromDistributor_SendResultBack()
        {
            const int distrServer1 = 22180;
            const int distrServer12 = 23180;
            const int storageServer1 = 22181;

            var writer =
                new HashWriter(new HashMapConfiguration("TestTransaction1D1S", HashMapCreationMode.CreateNew, 1, 1,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.Save();

            _distributor1.Build(1, distrServer1, distrServer12, "TestTransaction1D1S");
            _writer1.Build(storageServer1, "TestTransaction1D1S", 1);

            _distributor1.Start();
            _writer1.Start();

            var list = new List<InnerData>();
            const int count = 100;
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
                        Transaction = { TableName = "Int" }
                    };
                list.Add(ev);
            }

            foreach (var data in list)
            {
                _distributor1.Input.ProcessAsync(data);
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            foreach (var data in list)
            {
                var transaction = _distributor1.Main.GetTransactionState(data.Transaction.UserTransaction);
                Assert.AreEqual(TransactionState.Complete, transaction.State);
            }

            var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
            Assert.AreEqual(count, mem.Local);

            _writer1.Dispose();
            _distributor1.Dispose();
        }

        [TestMethod]
        public void Writer_ProcessDataFromDistributor_SendResultBack_TwoWriters()
        {
            const int distrServer1 = 22182;
            const int distrServer12 = 23182;
            const int storageServer1 = 22183;
            const int storageServer2 = 22184;

            var writer =
                new HashWriter(new HashMapConfiguration("TestTransaction1D2S", HashMapCreationMode.CreateNew, 2, 2,
                    HashFileType.Distributor));

            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.SetServer(1, "localhost", storageServer2, 157);
            writer.Save();

            _distributor1.Build(1, distrServer1, distrServer12, "TestTransaction1D2S");

            _writer1.Build(storageServer1, "TestTransaction1D2S", 1);
            _writer2.Build(storageServer2, "TestTransaction1D2S", 1);
         
            _distributor1.Start();

            _writer1.Start();            
            _writer2.Start();

            var list = new List<InnerData>();
            const int count = 100;
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
                        Transaction = { TableName = "Int" }
                    };

                list.Add(ev);
            }

            foreach (var data in list)
            {
                _distributor1.Input.ProcessAsync(data);
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(2000));

            foreach (var data in list)
            {
                var transaction = _distributor1.Main.GetTransactionState(data.Transaction.UserTransaction);
                Assert.AreEqual(TransactionState.Complete, transaction.State);
            }

            var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
            var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;

            Assert.AreEqual(count, mem.Local + mem2.Local);

            _writer1.Dispose();
            _writer2.Dispose();

            _distributor1.Dispose();        
        }

        [TestMethod]
        public void Writer_ProcessDataFromDistributor_SendResultBack_TwoWritersAndTwoReplics()
        {
            const int distrServer1 = 22185;
            const int distrServer12 = 23185;
            const int storageServer1 = 22186;
            const int storageServer2 = 22187;

            var writer =
                new HashWriter(new HashMapConfiguration("TestTransaction1D2S2Replics", HashMapCreationMode.CreateNew, 2,
                    2, HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.SetServer(1, "localhost", storageServer2, 157);
            writer.Save();

            _distributor1.Build(2, distrServer1, distrServer12, "TestTransaction1D2S2Replics");

            _writer1.Build(storageServer1, "TestTransaction1D2S2Replics",1);     
            _writer2.Build(storageServer2, "TestTransaction1D2S2Replics", 1);

            _distributor1.Start();

            _writer1.Start();
            _writer2.Start();

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
                        Transaction = { TableName = "Int" }
                    };

                list.Add(ev);
            }

            foreach (var data in list)
            {
                _distributor1.Input.ProcessAsync(data);
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(3000));

            foreach (var data in list)
            {
                var transaction = _distributor1.Main.GetTransactionState(data.Transaction.UserTransaction);
                Assert.AreEqual(TransactionState.Complete, transaction.State);
            }

            var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
            var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;

            Assert.AreEqual(count, mem.Local + mem2.Local);
            Assert.AreEqual(count, mem.Remote + mem2.Remote);
            
            _writer1.Dispose();
            _writer2.Dispose();

           _distributor1.Dispose();       
        }

        [TestMethod]
        public void Writer_ProcessDataFromDistributor_CRUD_TwoWriters()
        {            
            const int distrServer1 = 22201;
            const int distrServer12 = 22202;
            const int storageServer1 = 22203;
            const int storageServer2 = 22204;            

            var writer =
                new HashWriter(new HashMapConfiguration("TestCreateReadDelete", HashMapCreationMode.CreateNew, 2, 3,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.SetServer(1, "localhost", storageServer2, 157);
            writer.Save();

            #region hell

            var connection = new ConnectionConfiguration("testService", 10);            
            var distrconfig = new DistributorHashConfiguration(1);
            var queueconfig = new QueueConfiguration(2, 100);            

            var netReceive4 = new NetReceiverConfiguration(distrServer1, "localhost", "testService");
            var netReceive42 = new NetReceiverConfiguration(distrServer12, "localhost", "testService");

            var d = new DistributorSystem(new ServerId("localhost", distrServer1),
                new ServerId("localhost", distrServer12),
                distrconfig, queueconfig, connection,
                new DistributorCacheConfiguration(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(20)), netReceive4,
                netReceive42, new TransactionConfiguration(4),
                new HashMapConfiguration("TestCreateReadDelete", HashMapCreationMode.ReadFromFile,
                    1, 1, HashFileType.Distributor), new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            
            _writer1.Build(storageServer1, "TestCreateReadDelete",2);
            _writer2.Build(storageServer2, "TestCreateReadDelete",2);

            var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
            var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;
            
            _proxy.Start();

            d.Build();
            d.Start();

            _writer1.Start();
            _writer2.Start();

            _proxy.Distributor.SayIAmHere(new ServerId("localhost", distrServer12));

            #endregion

            const int count = 50;

            var api = _proxy.CreateApi("Int", false, new IntHashConvertor());

            for (int i = 0; i < count; i++)
            {
                var task = api.CreateSync(i, i);
                task.Wait();
                Assert.AreEqual(i + 1, mem.Local + mem2.Local);
            }

            for (int i = 0; i < count; i++)
            {
                UserTransaction user;
                var data = api.Read(i, out user);
                Assert.AreEqual(i, data);
            }

            for (int i = 0; i < count; i++)
            {
                var task = api.DeleteSync(i);
                task.Wait();
                Assert.AreEqual(count - i - 1, mem.Local + mem2.Local);
            }

            for (int i = 0; i < count; i++)
            {
                UserTransaction user;
                var data = api.Read(i, out user);
                Assert.IsNull(data);
            }

            _writer2.Dispose();
            _writer1.Dispose();

            d.Dispose();
            _proxy.Dispose();
        }
    }
}

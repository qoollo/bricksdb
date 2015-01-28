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
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DbController.AsyncDbWorks;
using Qoollo.Impl.DbController.Db;
using Qoollo.Impl.DbController.DbControllerNet;
using Qoollo.Impl.DistributorModules;
using Qoollo.Impl.DistributorModules.Caches;
using Qoollo.Impl.DistributorModules.DistributorNet;
using Qoollo.Impl.DistributorModules.ParallelWork;
using Qoollo.Impl.DistributorModules.Transaction;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestProxy;
using Qoollo.Tests.TestWriter;

namespace Qoollo.Tests
{
    [TestClass]
    public class TestWriterModules
    {
        [TestMethod]
        public void DbModule_LocalAndRemoteData_Count()
        {
            var provider = new IntHashConvertor();

            var writer =
                new HashWriter(new HashMapConfiguration("TestLocalAndRemote", HashMapCreationMode.CreateNew, 2, 3,
                    HashFileType.Collector));
            writer.CreateMap();
            writer.SetServer(0, "localhost", 54321, 157);
            writer.SetServer(1, "localhost", 11011, 157);
            writer.Save();

            var queueConfiguration = new QueueConfiguration(2, 1000);
            var hashMapConfiguration = new HashMapConfiguration("TestLocalAndRemote",
                HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Distributor);
            var local = new ServerId("localhost", 54321);

            var net = new DbControllerNetModule(new ConnectionConfiguration("", 10),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            
            var db = new DbModuleCollection();
            db.AddDbModule(new TestDbInMemory());

            var async = new AsyncTaskModule(new QueueConfiguration(1, 1));
            var restore = new AsyncDbWorkModule(net, async, db,
                new RestoreModuleConfiguration(10, TimeSpan.FromMinutes(100)),
                new RestoreModuleConfiguration(10, TimeSpan.FromMinutes(100)),
                new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)),
                new QueueConfiguration(1, 1), local);

            var distributor = new Impl.DbController.Distributor.DistributorModule(async, restore, net, local,
                hashMapConfiguration, new QueueConfiguration(2, 100), db);
            var main = new Impl.DbController.MainLogicModule(distributor, db);
            var input = new Impl.DbController.InputModule(main, queueConfiguration);

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
            distributor.Start();
            main.Start();
            input.Start();

            GlobalQueue.Queue.Start();

            foreach (var data in list)
            {
                input.Process(data);
            }

            Thread.Sleep(1000);

            var mem = db.GetDbModules.First() as TestDbInMemory;
            Assert.AreNotEqual(count, mem.Local);
            Assert.AreNotEqual(count, mem.Remote);
            Assert.AreEqual(count, mem.Local + mem.Remote);

            restore.Dispose();
            async.Dispose();
        }

        [TestMethod]
        public void Writer_SendRestoreCommandToDistributors_RestoreRemoteTable()
        {
            const int proxyServer = 22110;
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
                    HashFileType.Controller));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.SetServer(1, "localhost", storageServer2, 157);
            writer.Save();

            #region hell

            var q1 = new GlobalQueueInner();
            var q2 = new GlobalQueueInner();
            var q3 = new GlobalQueueInner();
            var q4 = new GlobalQueueInner();


            var queue = new QueueConfiguration(2, 100);
            var connection = new ConnectionConfiguration("testService", 10);
            var ndrc2 = new NetReceiverConfiguration(proxyServer, "localhost", "testService");
            var pcc = new ProxyCacheConfiguration(TimeSpan.FromSeconds(20));
            var pccc2 = new ProxyCacheConfiguration(TimeSpan.FromSeconds(4));

            var proxy = new TestProxySystem(new ServerId("localhost", proxyServer), queue,
                connection, pcc, pccc2, ndrc2,
                new AsyncTasksConfiguration(new TimeSpan()),
                new AsyncTasksConfiguration(new TimeSpan()),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));

            GlobalQueue.SetQueue(q1);

            var distrconfig = new DistributorHashConfiguration(2);
            var queueconfig = new QueueConfiguration(2, 100);
            var dnet = new DistributorNetModule(connection,
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            var ddistributor = new DistributorModule(new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)), distrconfig,
                queueconfig, dnet, new ServerId("localhost", distrServer1),
                new ServerId("localhost", distrServer12),
                new HashMapConfiguration("test7", HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Distributor));
            dnet.SetDistributor(ddistributor);

            var trans = new TransactionModule(new QueueConfiguration(1, 1000), dnet, new TransactionConfiguration(2),
                new DistributorHashConfiguration(2));
            var main =
                new MainLogicModule(new DistributorTimeoutCache(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(20)),
                    ddistributor, trans);
            var netReceive4 = new NetReceiverConfiguration(distrServer1, "localhost", "testService");
            var netReceive42 = new NetReceiverConfiguration(distrServer12, "localhost", "testService");
            var input = new InputModuleWithParallel(new QueueConfiguration(2, 100), main, trans);
            var receiver4 = new NetDistributorReceiver(main, input, ddistributor, netReceive4, netReceive42);

            GlobalQueue.SetQueue(q2);

            var dnet2 = new DistributorNetModule(connection,
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            var ddistributor2 = new DistributorModule(new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)), distrconfig,
                queueconfig, dnet2,
                new ServerId("localhost", distrServer2),
                new ServerId("localhost", distrServer22),
                new HashMapConfiguration("test6", HashMapCreationMode.ReadFromFile,
                    1, 2, HashFileType.Distributor));
            dnet2.SetDistributor(ddistributor2);

            var trans2 = new TransactionModule(new QueueConfiguration(1, 1000), dnet2, new TransactionConfiguration(2),
                new DistributorHashConfiguration(2));
            var main2 =
                new MainLogicModule(new DistributorTimeoutCache(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(20)),
                    ddistributor2,
                    trans2);
            var netReceive5 = new NetReceiverConfiguration(distrServer2, "localhost", "testService");
            var netReceive52 = new NetReceiverConfiguration(distrServer22, "localhost", "testService");
            var input2 = new InputModuleWithParallel(new QueueConfiguration(2, 100), main2, trans2);
            var receiver5 = new NetDistributorReceiver(main2, input2, ddistributor2, netReceive5, netReceive52);


            GlobalQueue.SetQueue(q3);

            var queueConfiguration = new QueueConfiguration(1, 1000);
            var hashMapConfiguration = new HashMapConfiguration("test6",
                HashMapCreationMode.ReadFromFile, 1, 2, HashFileType.Controller);
            var local = new ServerId("localhost", storageServer1);

            var net = new DbControllerNetModule(new ConnectionConfiguration("testService", 10),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            
            var db = new DbModuleCollection();
            db.AddDbModule(new TestDbInMemory());

            var async = new AsyncTaskModule(new QueueConfiguration(1, 1));
            var restore = new AsyncDbWorkModule(net, async, db,
                new RestoreModuleConfiguration(10, TimeSpan.FromMinutes(100)),
                new RestoreModuleConfiguration(10, TimeSpan.FromMinutes(100)),
                new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)),
                new QueueConfiguration(1, 1), local);

            var distributor = new Impl.DbController.Distributor.DistributorModule(async, restore, net, local,
                hashMapConfiguration, new QueueConfiguration(2, 10), db);
            var mainС = new Impl.DbController.MainLogicModule(distributor, db);
            var inputС = new Impl.DbController.InputModule(mainС, queueConfiguration);
            var netRc = new NetDbControllerReceiver(inputС, distributor,
                new NetReceiverConfiguration(storageServer1, "localhost", "testService"),
                new NetReceiverConfiguration(1, "fake", "fake"));

            GlobalQueue.SetQueue(q4);

            var hashMapConfiguration2 = new HashMapConfiguration("test7",
                HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Controller);
            var local2 = new ServerId("localhost", storageServer2);

            var net2 = new DbControllerNetModule(new ConnectionConfiguration("testService", 10),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));

            //var db2 = new TestDbInMemory();
            var db2 = new DbModuleCollection();
            db2.AddDbModule(new TestDbInMemory());

            var async2 = new AsyncTaskModule(new QueueConfiguration(1, 1));
            var restore2 = new AsyncDbWorkModule(net2, async2, db2,
                new RestoreModuleConfiguration(10, TimeSpan.FromMinutes(100)),
                new RestoreModuleConfiguration(10, TimeSpan.FromMinutes(100)),
                new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)),
                new QueueConfiguration(1, 1), local);

            var distributor2 = new Impl.DbController.Distributor.DistributorModule(async2, restore2, net2, local2,
                hashMapConfiguration2, new QueueConfiguration(2, 10), db2);
            var mainС2 = new Impl.DbController.MainLogicModule(distributor2, db2);
            var inputС2 = new Impl.DbController.InputModule(mainС2, queueConfiguration);
            var netRc2 = new NetDbControllerReceiver(inputС2, distributor2,
                new NetReceiverConfiguration(storageServer2, "localhost",
                    "testService"), new NetReceiverConfiguration(1, "fake", "fake"));

            #endregion

            proxy.Build();
            proxy.Start();

            receiver4.Start();
            receiver5.Start();

            input.Start();
            input2.Start();
            dnet.Start();
            dnet2.Start();
            ddistributor.Start();
            ddistributor2.Start();

            main.Start();
            main2.Start();

            q1.Start();
            q2.Start();


            proxy.Distributor.SayIAmHere(new ServerId("localhost", distrServer12));
            proxy.Distributor.SayIAmHere(new ServerId("localhost", distrServer22));

            ddistributor2.SayIAmHereRemoteResult(new ServerId("localhost", distrServer12));

            Thread.Sleep(TimeSpan.FromMilliseconds(300));
            Assert.AreEqual(1, ddistributor.GetDistributors().Count);
            Assert.AreEqual(1, ddistributor2.GetDistributors().Count);

            var api = proxy.CreateApi("Int", false, new IntHashConvertor());

            var tr1 = api.CreateSync(10, 10);
            var tr2 = api.CreateSync(11, 11);

            tr1.Wait();
            tr2.Wait();

            inputС.Start();
            inputС2.Start();
            distributor.Start();
            distributor2.Start();
            netRc.Start();
            netRc2.Start();

            q3.Start();
            q4.Start();

            distributor.Restore(new ServerId("localhost", distrServer1), false);            

            distributor2.Restore(new ServerId("localhost", distrServer2), false);

            Thread.Sleep(TimeSpan.FromMilliseconds(2000));

            var tr3 = api.CreateSync(12, 12);
            var tr4 = api.CreateSync(13, 13);

            tr3.Wait();
            tr4.Wait();

            var mem = db.GetDbModules.First() as TestDbInMemory;
            var mem2 = db2.GetDbModules.First() as TestDbInMemory;

            Assert.AreEqual(2, mem.Local);
            Assert.AreEqual(2, mem2.Local);

            proxy.Dispose();
            distributor.Dispose();
            distributor2.Dispose();
            ddistributor.Dispose();
            ddistributor2.Dispose();

            dnet.Dispose();
            dnet2.Dispose();
            net.Dispose();
            net2.Dispose();

            async.Dispose();
            async2.Dispose();

            restore.Dispose();
            restore2.Dispose();
        }

        [TestMethod]
        public void Writer_ProcessData_SendResultToDistributerMock()
        {
            const int distributorServer1 = 22171;
            const int storageServer1 = 22172;

            var q1 = new GlobalQueueInner();

            GlobalQueue.SetQueue(q1);

            var writer =
                new HashWriter(new HashMapConfiguration("TestDbTransaction", HashMapCreationMode.CreateNew, 1, 1,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.Save();

            #region hell

            var queueConfiguration = new QueueConfiguration(1, 1000);
            var hashMapConfiguration = new HashMapConfiguration("TestDbTransaction",
                HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Controller);
            var local = new ServerId("localhost", storageServer1);

            var net = new DbControllerNetModule(new ConnectionConfiguration("testService", 1),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            
            var db = new DbModuleCollection();
            db.AddDbModule(new TestDbInMemory());

            var async = new AsyncTaskModule(new QueueConfiguration(1, 10));
            var restore = new AsyncDbWorkModule(net, async, db,
                new RestoreModuleConfiguration(10, TimeSpan.FromMinutes(100)),
                new RestoreModuleConfiguration(10, TimeSpan.FromMilliseconds(100)),
                new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)),
                new QueueConfiguration(1, 1), local);

            var distributor = new Impl.DbController.Distributor.DistributorModule(async, restore, net, local,
                hashMapConfiguration, new QueueConfiguration(2, 1000), db);
            var mainС = new Impl.DbController.MainLogicModule(distributor, db);
            var inputС = new Impl.DbController.InputModule(mainС, queueConfiguration);
            var netRc = new NetDbControllerReceiver(inputС, distributor,
                new NetReceiverConfiguration(storageServer1, "localhost", "testService"),
                new NetReceiverConfiguration(1, "fake", "fake"));

            #endregion

            inputС.Start();
            distributor.Start();
            netRc.Start();
            async.Start();

            q1.Start();

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
                q1.DbInputProcessQueue.Add(data);
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            Assert.AreEqual(count, s.SendValue);
            net.Dispose();
            netRc.Dispose();
            async.Dispose();
        }

        [TestMethod]
        public void Writer_ProcessData_SendResultToTwoDistributeMocks()
        {
            const int distributorServer1 = 22173;
            const int distributorServer2 = 22174;
            const int storageServer1 = 22175;

            var q1 = new GlobalQueueInner();

            GlobalQueue.SetQueue(q1);

            var writer =
                new HashWriter(new HashMapConfiguration("TestDbTransaction2Distributors", HashMapCreationMode.CreateNew,
                    1, 1, HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.Save();

            #region hell

            var queueConfiguration = new QueueConfiguration(1, 1000);
            var hashMapConfiguration = new HashMapConfiguration("TestDbTransaction2Distributors",
                HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Controller);
            var local = new ServerId("localhost", storageServer1);

            var net = new DbControllerNetModule(new ConnectionConfiguration("testService", 1),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            
            var db = new DbModuleCollection();
            db.AddDbModule(new TestDbInMemory());

            var async = new AsyncTaskModule(new QueueConfiguration(1, 10));
            var restore = new AsyncDbWorkModule(net, async, db,
                new RestoreModuleConfiguration(10, TimeSpan.FromMinutes(100)),
                new RestoreModuleConfiguration(10, TimeSpan.FromMilliseconds(100)),
                new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)),
                new QueueConfiguration(1, 1), local);

            var distributor = new Impl.DbController.Distributor.DistributorModule(async, restore, net, local,
                hashMapConfiguration, new QueueConfiguration(2, 1000), db);
            var mainС = new Impl.DbController.MainLogicModule(distributor, db);
            var inputС = new Impl.DbController.InputModule(mainС, queueConfiguration);
            var netRc = new NetDbControllerReceiver(inputС, distributor,
                new NetReceiverConfiguration(storageServer1, "localhost", "testService"),
                new NetReceiverConfiguration(1, "fake", "fake"));

            #endregion

            inputС.Start();
            distributor.Start();
            netRc.Start();
            async.Start();            

            q1.Start();

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
                q1.DbInputProcessQueue.Add(data);
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            Assert.AreEqual(count, s.SendValue);
            Assert.AreEqual(count, s2.SendValue);

            async.Dispose();
        }

        [TestMethod]
        public void Writer_ProcessDataFromDistributor_SendResultBack()
        {
            const int distrServer1 = 22180;
            const int distrServer12 = 23180;
            const int storageServer1 = 22181;

            var q1 = new GlobalQueueInner();

            var writer =
                new HashWriter(new HashMapConfiguration("TestTransaction1D1S", HashMapCreationMode.CreateNew, 1, 1,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.Save();

            #region hell

            GlobalQueue.SetQueue(q1);

            var connection = new ConnectionConfiguration("testService", 10);
            var distrconfig = new DistributorHashConfiguration(1);
            var queueconfig = new QueueConfiguration(1, 100);
            var dnet = new DistributorNetModule(connection,
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            var ddistributor = new DistributorModule(new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)), distrconfig,
                queueconfig, dnet,
                new ServerId("localhost", distrServer1),
                new ServerId("localhost", distrServer12),
                new HashMapConfiguration("TestTransaction1D1S",
                    HashMapCreationMode.ReadFromFile,
                    1, 1, HashFileType.Distributor));
            dnet.SetDistributor(ddistributor);

            var tranc = new TransactionModule(new QueueConfiguration(1, 1000), dnet, new TransactionConfiguration(4),
                distrconfig);
            var main =
                new MainLogicModule(new DistributorTimeoutCache(TimeSpan.FromSeconds(200), TimeSpan.FromSeconds(200)),
                    ddistributor, tranc);

            var netReceive4 = new NetReceiverConfiguration(distrServer1, "localhost", "testService");
            var netReceive42 = new NetReceiverConfiguration(distrServer12, "localhost", "testService");
            var input = new InputModuleWithParallel(new QueueConfiguration(2, 100000), main, tranc);
            var receiver4 = new NetDistributorReceiver(main, input, ddistributor, netReceive4, netReceive42);


            var queueConfiguration = new QueueConfiguration(1, 1000);
            var hashMapConfiguration = new HashMapConfiguration("TestTransaction1D1S",
                HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Controller);
            var local = new ServerId("localhost", storageServer1);

            var net = new DbControllerNetModule(new ConnectionConfiguration("testService", 1),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));

            var db = new DbModuleCollection();
            db.AddDbModule(new TestDbInMemory());

            var async = new AsyncTaskModule(new QueueConfiguration(1, 10));
            var restore = new AsyncDbWorkModule(net, async, db,
                new RestoreModuleConfiguration(10, TimeSpan.FromMinutes(100)),
                new RestoreModuleConfiguration(10, TimeSpan.FromMilliseconds(100)),
                new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)),
                new QueueConfiguration(1, 1), local);

            var distributor = new Impl.DbController.Distributor.DistributorModule(async, restore, net, local,
                hashMapConfiguration, new QueueConfiguration(2, 10), db);
            var mainС = new Impl.DbController.MainLogicModule(distributor, db);
            var inputС = new Impl.DbController.InputModule(mainС, queueConfiguration);
            var netRc = new NetDbControllerReceiver(inputС, distributor,
                new NetReceiverConfiguration(storageServer1, "localhost", "testService"),
                new NetReceiverConfiguration(1, "fake", "fake"));

            #endregion

            main.Start();
            receiver4.Start();
            input.Start();
            dnet.Start();
            ddistributor.Start();

            inputС.Start();
            distributor.Start();
            netRc.Start();
            async.Start();

            q1.Start();

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
                input.ProcessAsync(data);
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            foreach (var data in list)
            {
                var transaction = main.GetTransactionState(data.Transaction.UserTransaction);
                Assert.AreEqual(TransactionState.Complete, transaction.State);
            }

            var mem = db.GetDbModules.First() as TestDbInMemory;
            Assert.AreEqual(count, mem.Local);

            restore.Dispose();
            async.Dispose();
            distributor.Dispose();
            ddistributor.Dispose();
        }

        [TestMethod]
        public void Writer_ProcessDataFromDistributor_SendResultBack_TwoWriters()
        {
            const int distrServer1 = 22182;
            const int distrServer12 = 23182;
            const int storageServer1 = 22183;
            const int storageServer2 = 22184;

            var q1 = new GlobalQueueInner();
            var q2 = new GlobalQueueInner();

            var writer =
                new HashWriter(new HashMapConfiguration("TestTransaction1D2S", HashMapCreationMode.CreateNew, 2, 2,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.SetServer(1, "localhost", storageServer2, 157);
            writer.Save();

            #region hell

            GlobalQueue.SetQueue(q1);

            var connection = new ConnectionConfiguration("testService", 10);

            var distrconfig = new DistributorHashConfiguration(1);
            var queueconfig = new QueueConfiguration(1, 100);
            var dnet = new DistributorNetModule(connection,
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            var ddistributor = new DistributorModule(new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)), distrconfig,
                queueconfig, dnet,
                new ServerId("localhost", distrServer1),
                new ServerId("localhost", distrServer12),
                new HashMapConfiguration("TestTransaction1D2S",
                    HashMapCreationMode.ReadFromFile,
                    1, 1, HashFileType.Distributor));
            dnet.SetDistributor(ddistributor);

            var tranc = new TransactionModule(new QueueConfiguration(1, 1000), dnet, new TransactionConfiguration(4),
                distrconfig);
            var main =
                new MainLogicModule(new DistributorTimeoutCache(TimeSpan.FromSeconds(200), TimeSpan.FromSeconds(200)),
                    ddistributor, tranc);

            var netReceive4 = new NetReceiverConfiguration(distrServer1, "localhost", "testService");
            var netReceive42 = new NetReceiverConfiguration(distrServer12, "localhost", "testService");
            var input = new InputModuleWithParallel(new QueueConfiguration(2, 100000), main, tranc);
            var receiver4 = new NetDistributorReceiver(main, input, ddistributor, netReceive4, netReceive42);

            var queueConfiguration = new QueueConfiguration(1, 1000);
            var hashMapConfiguration = new HashMapConfiguration("TestTransaction1D2S",
                HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Controller);
            var local = new ServerId("localhost", storageServer1);

            var net = new DbControllerNetModule(new ConnectionConfiguration("testService", 1),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));

            //var db = new TestDbInMemory();
            var db = new DbModuleCollection();
            db.AddDbModule(new TestDbInMemory());

            var async = new AsyncTaskModule(new QueueConfiguration(1, 10));
            var restore = new AsyncDbWorkModule(net, async, db,
                new RestoreModuleConfiguration(10, TimeSpan.FromMinutes(100)),
                new RestoreModuleConfiguration(10, TimeSpan.FromMilliseconds(100)),
                new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)),
                new QueueConfiguration(1, 1), local);

            var distributor = new Impl.DbController.Distributor.DistributorModule(async, restore, net, local,
                hashMapConfiguration, new QueueConfiguration(2, 10), db);
            var mainС = new Impl.DbController.MainLogicModule(distributor, db);
            var inputС = new Impl.DbController.InputModule(mainС, queueConfiguration);
            var netRc = new NetDbControllerReceiver(inputС, distributor,
                new NetReceiverConfiguration(storageServer1, "localhost", "testService"),
                new NetReceiverConfiguration(1, "fake", "fake"));

            GlobalQueue.SetQueue(q2);

            var queueConfiguration2 = new QueueConfiguration(1, 1000);
            var hashMapConfiguration2 = new HashMapConfiguration("TestTransaction1D2S",
                HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Controller);
            var local2 = new ServerId("localhost", storageServer2);

            var net2 = new DbControllerNetModule(new ConnectionConfiguration("testService", 1),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));

            //var db2 = new TestDbInMemory();
            var db2 = new DbModuleCollection();
            db2.AddDbModule(new TestDbInMemory());

            var async2 = new AsyncTaskModule(new QueueConfiguration(1, 10));
            var restore2 = new AsyncDbWorkModule(net2, async2, db2,
                new RestoreModuleConfiguration(10, TimeSpan.FromMinutes(100)),
                new RestoreModuleConfiguration(10, TimeSpan.FromMilliseconds(100)),
                new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)),
                new QueueConfiguration(1, 1), local);

            var distributor2 = new Impl.DbController.Distributor.DistributorModule(async2, restore2, net2, local2,
                hashMapConfiguration2, new QueueConfiguration(2, 10), db2);
            var mainС2 = new Impl.DbController.MainLogicModule(distributor2, db2);
            var inputС2 = new Impl.DbController.InputModule(mainС2, queueConfiguration2);
            var netRc2 = new NetDbControllerReceiver(inputС2, distributor2,
                new NetReceiverConfiguration(storageServer2, "localhost", "testService"),
                new NetReceiverConfiguration(1, "fake", "fake"));

            #endregion

            main.Start();
            receiver4.Start();
            input.Start();
            dnet.Start();
            ddistributor.Start();

            inputС.Start();
            distributor.Start();
            netRc.Start();
            async.Start();

            q1.Start();

            inputС2.Start();
            distributor2.Start();
            netRc2.Start();
            async2.Start();

            q2.Start();

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
                input.ProcessAsync(data);
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(2000));

            foreach (var data in list)
            {
                var transaction = main.GetTransactionState(data.Transaction.UserTransaction);
                Assert.AreEqual(TransactionState.Complete, transaction.State);
            }

            var mem = db.GetDbModules.First() as TestDbInMemory;
            var mem2 = db2.GetDbModules.First() as TestDbInMemory;

            Assert.AreEqual(count, mem.Local + mem2.Local);

            ddistributor.Dispose();
            restore.Dispose();
            restore2.Dispose();
            async.Dispose();
            async2.Dispose();
        }

        [TestMethod]
        public void Writer_ProcessDataFromDistributor_SendResultBack_TwoWritersAndTwoReplics()
        {
            const int distrServer1 = 22185;
            const int distrServer12 = 23185;
            const int storageServer1 = 22186;
            const int storageServer2 = 22187;

            var q1 = new GlobalQueueInner();
            var q2 = new GlobalQueueInner();

            var writer =
                new HashWriter(new HashMapConfiguration("TestTransaction1D2S2Replics", HashMapCreationMode.CreateNew, 2,
                    2, HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.SetServer(1, "localhost", storageServer2, 157);
            writer.Save();

            #region hell

            GlobalQueue.SetQueue(q1);

            var connection = new ConnectionConfiguration("testService", 10);

            var distrconfig = new DistributorHashConfiguration(2);
            var queueconfig = new QueueConfiguration(1, 100);
            var dnet = new DistributorNetModule(connection,
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            var ddistributor = new DistributorModule(new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)), distrconfig,
                queueconfig, dnet,
                new ServerId("localhost", distrServer1),
                new ServerId("localhost", distrServer12),
                new HashMapConfiguration("TestTransaction1D2S2Replics",
                    HashMapCreationMode.ReadFromFile,
                    1, 2, HashFileType.Distributor));
            dnet.SetDistributor(ddistributor);

            var tranc = new TransactionModule(new QueueConfiguration(1, 1000), dnet, new TransactionConfiguration(4),
                distrconfig);
            var main =
                new MainLogicModule(new DistributorTimeoutCache(TimeSpan.FromSeconds(200), TimeSpan.FromSeconds(200)),
                    ddistributor, tranc);

            var netReceive4 = new NetReceiverConfiguration(distrServer1, "localhost", "testService");
            var netReceive42 = new NetReceiverConfiguration(distrServer12, "localhost", "testService");
            var input = new InputModuleWithParallel(new QueueConfiguration(2, 100000), main, tranc);
            var receiver4 = new NetDistributorReceiver(main, input, ddistributor, netReceive4, netReceive42);


            var queueConfiguration = new QueueConfiguration(1, 1000);
            var hashMapConfiguration = new HashMapConfiguration("TestTransaction1D2S2Replics",
                HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Controller);
            var local = new ServerId("localhost", storageServer1);

            var net = new DbControllerNetModule(new ConnectionConfiguration("testService", 1),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            
            var db = new DbModuleCollection();
            db.AddDbModule(new TestDbInMemory());

            var async = new AsyncTaskModule(new QueueConfiguration(1, 10));
            var restore = new AsyncDbWorkModule(net, async, db,
                new RestoreModuleConfiguration(10, TimeSpan.FromMinutes(100)),
                new RestoreModuleConfiguration(10, TimeSpan.FromMilliseconds(100)),
                new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)),
                new QueueConfiguration(1, 1), local);

            var distributor = new Impl.DbController.Distributor.DistributorModule(async, restore, net, local,
                hashMapConfiguration, new QueueConfiguration(2, 10), db);
            var mainС = new Impl.DbController.MainLogicModule(distributor, db);
            var inputС = new Impl.DbController.InputModule(mainС, queueConfiguration);
            var netRc = new NetDbControllerReceiver(inputС, distributor,
                new NetReceiverConfiguration(storageServer1, "localhost", "testService"),
                new NetReceiverConfiguration(1, "fake", "fake"));

            GlobalQueue.SetQueue(q2);

            var queueConfiguration2 = new QueueConfiguration(1, 1000);
            var hashMapConfiguration2 = new HashMapConfiguration("TestTransaction1D2S2Replics",
                HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Controller);
            var local2 = new ServerId("localhost", storageServer2);

            var net2 = new DbControllerNetModule(new ConnectionConfiguration("testService", 1),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            
            var db2 = new DbModuleCollection();
            db2.AddDbModule(new TestDbInMemory());

            var async2 = new AsyncTaskModule(new QueueConfiguration(1, 10));
            var restore2 = new AsyncDbWorkModule(net2, async2, db2,
                new RestoreModuleConfiguration(10, TimeSpan.FromMinutes(100)),
                new RestoreModuleConfiguration(10, TimeSpan.FromMilliseconds(100)),
                new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)),
                new QueueConfiguration(1, 1), local);

            var distributor2 = new Impl.DbController.Distributor.DistributorModule(async2, restore2, net2, local2,
                hashMapConfiguration2, new QueueConfiguration(2, 10), db2);
            var mainС2 = new Impl.DbController.MainLogicModule(distributor2, db2);
            var inputС2 = new Impl.DbController.InputModule(mainС2, queueConfiguration2);
            var netRc2 = new NetDbControllerReceiver(inputС2, distributor2,
                new NetReceiverConfiguration(storageServer2, "localhost", "testService"),
                new NetReceiverConfiguration(1, "fake", "fake"));

            #endregion

            main.Start();
            receiver4.Start();
            input.Start();
            dnet.Start();
            ddistributor.Start();

            inputС.Start();
            distributor.Start();
            netRc.Start();
            async.Start();

            q1.Start();

            inputС2.Start();
            distributor2.Start();
            netRc2.Start();
            async2.Start();

            q2.Start();

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
                input.ProcessAsync(data);
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(3000));

            foreach (var data in list)
            {
                var transaction = main.GetTransactionState(data.Transaction.UserTransaction);
                Assert.AreEqual(TransactionState.Complete, transaction.State);
            }

            var mem = db.GetDbModules.First() as TestDbInMemory;
            var mem2 = db2.GetDbModules.First() as TestDbInMemory;

            Assert.AreEqual(count, mem.Local + mem2.Local);
            Assert.AreEqual(count, mem.Remote + mem2.Remote);

            restore.Dispose();
            restore2.Dispose();
            ddistributor.Dispose();
            async.Dispose();
            async2.Dispose();
        }

        [TestMethod]
        public void Writer_ProcessDataFromDistributor_CRUD_TwoWriters()
        {
            const int proxyServer = 22020;
            const int distrServer1 = 22201;
            const int distrServer12 = 22202;
            const int storageServer1 = 22203;
            const int storageServer2 = 22204;

            var q1 = new GlobalQueueInner();
            var q2 = new GlobalQueueInner();

            GlobalQueue.SetQueue(q1);

            var writer =
                new HashWriter(new HashMapConfiguration("TestCreateReadDelete", HashMapCreationMode.CreateNew, 2, 3,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.SetServer(1, "localhost", storageServer2, 157);
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


            var distrconfig = new DistributorHashConfiguration(1);
            var queueconfig = new QueueConfiguration(2, 100);
            var dnet = new DistributorNetModule(connection,
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            var ddistributor = new DistributorModule(new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)), distrconfig,
                queueconfig, dnet,
                new ServerId("localhost", distrServer1),
                new ServerId("localhost", distrServer12),
                new HashMapConfiguration("TestCreateReadDelete", HashMapCreationMode.ReadFromFile,
                    1, 1, HashFileType.Distributor));
            dnet.SetDistributor(ddistributor);

            var tranc = new TransactionModule(new QueueConfiguration(1, 1000), dnet, new TransactionConfiguration(4),
                distrconfig);

            var distrcache = new DistributorTimeoutCache(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(20));
            var main = new MainLogicModule(distrcache, ddistributor, tranc);

            distrcache.SetMainLogicModule(main);

            var netReceive4 = new NetReceiverConfiguration(distrServer1, "localhost", "testService");
            var netReceive42 = new NetReceiverConfiguration(distrServer12, "localhost", "testService");
            var input = new InputModuleWithParallel(new QueueConfiguration(4, 100000), main, tranc);
            var receiver4 = new NetDistributorReceiver(main, input, ddistributor, netReceive4, netReceive42);


            var queueConfiguration = new QueueConfiguration(1, 1000);
            var hashMapConfiguration = new HashMapConfiguration("TestCreateReadDelete",
                HashMapCreationMode.ReadFromFile, 1, 2, HashFileType.Controller);
            var local = new ServerId("localhost", storageServer1);

            var net = new DbControllerNetModule(new ConnectionConfiguration("testService", 10),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            
            var db = new DbModuleCollection();
            db.AddDbModule(new TestDbInMemory());

            var async = new AsyncTaskModule(new QueueConfiguration(1, 10));
            var restore = new AsyncDbWorkModule(net, async, db,
                new RestoreModuleConfiguration(10, TimeSpan.FromMinutes(100)),
                new RestoreModuleConfiguration(10, TimeSpan.FromMilliseconds(100)),
                new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)),
                new QueueConfiguration(1, 100), local);

            var distributor = new Impl.DbController.Distributor.DistributorModule(async, restore, net, local,
                hashMapConfiguration, new QueueConfiguration(2, 10), db);
            var mainС = new Impl.DbController.MainLogicModule(distributor, db);
            var inputС = new Impl.DbController.InputModule(mainС, queueConfiguration);
            var netRc = new NetDbControllerReceiver(inputС, distributor,
                new NetReceiverConfiguration(storageServer1, "localhost", "testService"),
                new NetReceiverConfiguration(1, "fake", "fake"));

            GlobalQueue.SetQueue(q2);

            var hashMapConfiguration2 = new HashMapConfiguration("TestCreateReadDelete",
                HashMapCreationMode.ReadFromFile, 1, 2, HashFileType.Controller);
            var local2 = new ServerId("localhost", storageServer2);

            var net2 = new DbControllerNetModule(new ConnectionConfiguration("testService", 10),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            
            var db2 = new DbModuleCollection();
            db2.AddDbModule(new TestDbInMemory());

            var async2 = new AsyncTaskModule(new QueueConfiguration(1, 10));
            var restore2 = new AsyncDbWorkModule(net2, async2, db2,
                new RestoreModuleConfiguration(10, TimeSpan.FromMinutes(100)),
                new RestoreModuleConfiguration(10, TimeSpan.FromMilliseconds(100)),
                new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)),
                new QueueConfiguration(1, 100), local);

            var distributor2 = new Impl.DbController.Distributor.DistributorModule(async2, restore2, net2, local2,
                hashMapConfiguration2, new QueueConfiguration(2, 10), db2);
            var mainС2 = new Impl.DbController.MainLogicModule(distributor2, db2);
            var inputС2 = new Impl.DbController.InputModule(mainС2, queueConfiguration);
            var netRc2 = new NetDbControllerReceiver(inputС2, distributor2,
                new NetReceiverConfiguration(storageServer2, "localhost", "testService"),
                new NetReceiverConfiguration(1, "fake", "fake"));


            #endregion

            var mem = db.GetDbModules.First() as TestDbInMemory;
            var mem2 = db2.GetDbModules.First() as TestDbInMemory;

            #region hell2

            proxy.Build();
            proxy.Start();

            main.Start();
            receiver4.Start();
            input.Start();
            dnet.Start();
            ddistributor.Start();

            inputС.Start();
            distributor.Start();
            netRc.Start();
            async.Start();

            q1.Start();

            inputС2.Start();
            distributor2.Start();
            netRc2.Start();
            async2.Start();

            q2.Start();
            proxy.Distributor.SayIAmHere(new ServerId("localhost", distrServer12));

            #endregion

            const int count = 50;

            var api = proxy.CreateApi("Int", false, new IntHashConvertor());

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

            q1.Dispose();

            distributor.Dispose();
            distributor2.Dispose();
            ddistributor.Dispose();
            async.Dispose();
            async2.Dispose();
            dnet.Dispose();
            net.Dispose();
            net2.Dispose();

            proxy.Dispose();
        }
    }
}

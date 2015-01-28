using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Client.Configuration;
using Qoollo.Client.DistributorGate;
using Qoollo.Client.Request;
using Qoollo.Client.StorageGate;
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
using Qoollo.Tests.TestModules;
using Qoollo.Tests.TestProxy;
using Qoollo.Tests.TestWriter;
using Consts = Qoollo.Impl.Common.Support.Consts;

namespace Qoollo.Tests
{
    [TestClass]
    public class TestRestore
    {
        [TestMethod]
        public void Writer_Restore_TwoServers()
        {
            const int distrServer1 = 22123;
            const int distrServer12 = 23123;
            const int storageServer1 = 22125;
            const int storageServer2 = 22126;

            var q1 = new GlobalQueueInner();
            var q2 = new GlobalQueueInner();

            var writer =
                new HashWriter(new HashMapConfiguration("test8", HashMapCreationMode.CreateNew, 2, 3,
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
                new HashMapConfiguration("test8", HashMapCreationMode.ReadFromFile,
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
            var hashMapConfiguration = new HashMapConfiguration("test8",
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
                new NetReceiverConfiguration(storageServer1, "localhost",
                    "testService"),
                new NetReceiverConfiguration(1, "fake", "fake"));

            GlobalQueue.SetQueue(q2);

            var hashMapConfiguration2 = new HashMapConfiguration("test8",
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
                new QueueConfiguration(1, 10000), local);


            var distributor2 = new Impl.DbController.Distributor.DistributorModule(async2, restore2, net2, local2,
                hashMapConfiguration2, new QueueConfiguration(2, 10), db2);
            var mainС2 = new Impl.DbController.MainLogicModule(distributor2, db2);
            var inputС2 = new Impl.DbController.InputModule(mainС2, queueConfiguration);
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
                input.ProcessAsync(data);
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(6000));

            foreach (var data in list)
            {
                var tr = main.GetTransactionState(data.Transaction.UserTransaction);
                if (tr.State != TransactionState.Complete)
                {
                    data.Transaction = new Transaction(data.Transaction);
                    data.Transaction.ClearError();
                    input.ProcessAsync(data);
                }
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(5000));

            var mem = db.GetDbModules.First() as TestDbInMemory;
            var mem2 = db2.GetDbModules.First() as TestDbInMemory;

            if (count > 1)
            {
                Assert.AreNotEqual(count, mem.Local);
                Assert.AreNotEqual(count, mem.Remote);
            }
            Assert.AreEqual(count, mem.Local + mem.Remote);

            inputС2.Start();
            distributor2.Start();
            netRc2.Start();
            async2.Start();

            q2.Start();

            distributor2.Restore(new ServerId("localhost", distrServer1), false);

            Thread.Sleep(TimeSpan.FromMilliseconds(2000));

            Assert.AreEqual(0, mem.Remote);
            Assert.AreEqual(0, mem2.Remote);
            Assert.AreEqual(count, mem.Local + mem2.Local);
            Assert.AreEqual(false, restore.IsNeedRestore);
            Assert.AreEqual(false, restore2.IsNeedRestore);

            q1.Dispose();

            distributor.Dispose();
            distributor2.Dispose();
            ddistributor.Dispose();
            async.Dispose();
            async2.Dispose();
            net.Dispose();
            net2.Dispose();
            dnet.Dispose();
            restore.Dispose();
            restore2.Dispose();
        }

        [TestMethod]
        public void Writer_Restore_ThreeServers()
        {
            const int proxyServer = 22154;
            const int distrServer1 = 22153;
            const int distrServer12 = 23153;
            const int storageServer1 = 22357;
            const int storageServer2 = 22156;
            const int storageServer3 = 22157;

            var q1 = new GlobalQueueInner();
            var q2 = new GlobalQueueInner();
            var q3 = new GlobalQueueInner();
            var q4 = new GlobalQueueInner();

            GlobalQueue.SetQueue(q1);

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


            var distrconfig = new DistributorHashConfiguration(1);
            var queueconfig = new QueueConfiguration(2, 100);
            var dnet = new DistributorNetModule(connection,
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            var ddistributor = new DistributorModule(new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)), distrconfig,
                queueconfig, dnet,
                new ServerId("localhost", distrServer1),
                new ServerId("localhost", distrServer12),
                new HashMapConfiguration("test11", HashMapCreationMode.ReadFromFile,
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

            GlobalQueue.SetQueue(q2);

            var queueConfiguration = new QueueConfiguration(1, 1000);
            var hashMapConfiguration = new HashMapConfiguration("test11",
                HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Controller);
            var local = new ServerId("localhost", storageServer1);

            var net =
                new DbControllerNetModule(
                    new ConnectionConfiguration("testService", 10),
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

            GlobalQueue.SetQueue(q3);

            var hashMapConfiguration2 = new HashMapConfiguration("test11",
                HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Controller);
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
                new QueueConfiguration(1, 100), local2);

            var distributor2 = new Impl.DbController.Distributor.DistributorModule(async2, restore2, net2, local2,
                hashMapConfiguration2, new QueueConfiguration(2, 10), db2);
            var mainС2 = new Impl.DbController.MainLogicModule(distributor2, db2);
            var inputС2 = new Impl.DbController.InputModule(mainС2, queueConfiguration);
            var netRc2 = new NetDbControllerReceiver(inputС2, distributor2,
                new NetReceiverConfiguration(storageServer2, "localhost", "testService"),
                new NetReceiverConfiguration(1, "fake", "fake"));

            GlobalQueue.SetQueue(q4);

            var hashMapConfiguration3 = new HashMapConfiguration("test11",
                HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Controller);
            var local3 = new ServerId("localhost", storageServer3);

            var net3 = new DbControllerNetModule(new ConnectionConfiguration("testService", 10),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));

            //var db3 = new TestDbInMemory();
            var db3 = new DbModuleCollection();
            db3.AddDbModule(new TestDbInMemory());

            var async3 = new AsyncTaskModule(new QueueConfiguration(1, 10));
            var restore3 = new AsyncDbWorkModule(net3, async3, db3,
                new RestoreModuleConfiguration(10, TimeSpan.FromMinutes(100)),
                new RestoreModuleConfiguration(10, TimeSpan.FromMilliseconds(100)),
                new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)),
                new QueueConfiguration(1, 100), local3);

            var distributor3 = new Impl.DbController.Distributor.DistributorModule(async3, restore3, net3, local3,
                hashMapConfiguration3, new QueueConfiguration(2, 10), db3);
            var mainС3 = new Impl.DbController.MainLogicModule(distributor3, db3);
            var inputС3 = new Impl.DbController.InputModule(mainС3, queueConfiguration);
            var netRc3 = new NetDbControllerReceiver(inputС3, distributor3,
                new NetReceiverConfiguration(storageServer3, "localhost", "testService"),
                new NetReceiverConfiguration(1, "fake", "fake"));

            #endregion

            #region hell2

            proxy.Build();
            proxy.Start();

            main.Start();
            receiver4.Start();
            input.Start();
            dnet.Start();
            ddistributor.Start();

            q1.Start();

            inputС.Start();
            distributor.Start();
            netRc.Start();
            async.Start();

            q2.Start();

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

            var mem = db.GetDbModules.First() as TestDbInMemory;
            var mem2 = db2.GetDbModules.First() as TestDbInMemory;
            var mem3 = db3.GetDbModules.First() as TestDbInMemory;

            if (count > 1)
            {
                Assert.AreNotEqual(count, mem.Local);
                Assert.AreNotEqual(count, mem.Remote);
            }
            Assert.AreEqual(count, mem.Local + mem.Remote);

            inputС2.Start();
            distributor2.Start();
            netRc2.Start();
            async2.Start();

            q3.Start();

            inputС3.Start();
            distributor3.Start();
            netRc3.Start();
            async3.Start();

            q4.Start();

            distributor2.Restore(new ServerId("localhost", distrServer1), false);

            Thread.Sleep(TimeSpan.FromMilliseconds(4000));

            distributor3.Restore(new ServerId("localhost", distrServer1), false);

            Thread.Sleep(TimeSpan.FromMilliseconds(3000));

            Assert.AreEqual(0, mem.Remote);
            Assert.AreEqual(0, mem2.Remote);
            Assert.AreEqual(0, mem3.Remote);
            Assert.AreEqual(count, mem.Local + mem2.Local + mem3.Local);
            Assert.AreEqual(false, restore.IsNeedRestore);
            Assert.AreEqual(false, restore2.IsNeedRestore);
            Assert.AreEqual(false, restore3.IsNeedRestore);

            q1.Dispose();

            distributor.Dispose();
            distributor2.Dispose();
            distributor3.Dispose();
            ddistributor.Dispose();
            async.Dispose();
            async2.Dispose();
            async3.Dispose();

            dnet.Dispose();
            net.Dispose();
            net2.Dispose();
            net3.Dispose();

            proxy.Dispose();
        }

        [TestMethod]
        public void Writer_RestoreAfterUpdateHashFile_ThreeServers()
        {
            const int distrServer1 = 22143;
            const int distrServer12 = 23143;
            const int storageServer1 = 22145;
            const int storageServer2 = 22146;
            const int storageServer3 = 22147;

            var q1 = new GlobalQueueInner();
            var q2 = new GlobalQueueInner();
            var q3 = new GlobalQueueInner();
            var q4 = new GlobalQueueInner();

            var writer =
                new HashWriter(new HashMapConfiguration("TestRestore3ServersUpdate", HashMapCreationMode.CreateNew, 2, 3,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.SetServer(1, "localhost", storageServer2, 157);
            writer.Save();

            #region hell

            GlobalQueue.SetQueue(q1);

            var connection = new ConnectionConfiguration("testService", 10);

            var distrconfig = new DistributorHashConfiguration(1);
            var queueconfig = new QueueConfiguration(2, 100);
            var dnet = new DistributorNetModule(connection,
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            var ddistributor = new DistributorModule(new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)), distrconfig,
                queueconfig, dnet,
                new ServerId("localhost", distrServer1),
                new ServerId("localhost", distrServer12),
                new HashMapConfiguration("TestRestore3ServersUpdate", HashMapCreationMode.ReadFromFile,
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

            GlobalQueue.SetQueue(q2);

            var queueConfiguration = new QueueConfiguration(1, 1000);
            var hashMapConfiguration = new HashMapConfiguration("TestRestore3ServersUpdate",
                HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Controller);
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
                new QueueConfiguration(1, 10000), local);

            var distributor = new Impl.DbController.Distributor.DistributorModule(async, restore, net, local,
                hashMapConfiguration, new QueueConfiguration(2, 10), db);
            var mainС = new Impl.DbController.MainLogicModule(distributor, db);
            var inputС = new Impl.DbController.InputModule(mainС, queueConfiguration);
            var netRc = new NetDbControllerReceiver(inputС, distributor,
                new NetReceiverConfiguration(storageServer1, "localhost", "testService"),
                new NetReceiverConfiguration(1, "fake", "fake"));

            GlobalQueue.SetQueue(q3);

            var hashMapConfiguration2 = new HashMapConfiguration("TestRestore3ServersUpdate",
                HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Controller);
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
                new QueueConfiguration(1, 10000), local2);

            var distributor2 = new Impl.DbController.Distributor.DistributorModule(async2, restore2, net2, local2,
                hashMapConfiguration2, new QueueConfiguration(2, 10), db2);
            var mainС2 = new Impl.DbController.MainLogicModule(distributor2, db2);
            var inputС2 = new Impl.DbController.InputModule(mainС2, queueConfiguration);
            var netRc2 = new NetDbControllerReceiver(inputС2, distributor2,
                new NetReceiverConfiguration(storageServer2, "localhost", "testService"),
                new NetReceiverConfiguration(1, "fake", "fake"));

            GlobalQueue.SetQueue(q4);

            var hashMapConfiguration3 = new HashMapConfiguration("TestRestore3ServersUpdate",
                HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Controller);
            var local3 = new ServerId("localhost", storageServer3);

            var net3 = new DbControllerNetModule(new ConnectionConfiguration("testService", 10),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            
            var db3 = new DbModuleCollection();
            db3.AddDbModule(new TestDbInMemory());

            var async3 = new AsyncTaskModule(new QueueConfiguration(1, 10));
            var restore3 = new AsyncDbWorkModule(net3, async3, db3,
                new RestoreModuleConfiguration(10, TimeSpan.FromMinutes(100)),
                new RestoreModuleConfiguration(10, TimeSpan.FromMilliseconds(100)),
                new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)),
                new QueueConfiguration(1, 10000), local3);

            var distributor3 = new Impl.DbController.Distributor.DistributorModule(async3, restore3, net3, local3,
                hashMapConfiguration3, new QueueConfiguration(2, 10), db3);
            var mainС3 = new Impl.DbController.MainLogicModule(distributor3, db3);
            var inputС3 = new Impl.DbController.InputModule(mainС3, queueConfiguration);
            var netRc3 = new NetDbControllerReceiver(inputС3, distributor3,
                new NetReceiverConfiguration(storageServer3, "localhost", "testService"),
                new NetReceiverConfiguration(1, "fake", "fake"));

            #endregion

            #region hell2

            main.Start();
            receiver4.Start();
            input.Start();
            dnet.Start();
            ddistributor.Start();

            q1.Start();

            inputС.Start();
            distributor.Start();
            netRc.Start();
            async.Start();

            q2.Start();

            inputС2.Start();
            distributor2.Start();
            netRc2.Start();
            async2.Start();

            q3.Start();

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
                input.ProcessAsync(data);
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(5000));

            #endregion

            var mem = db.GetDbModules.First() as TestDbInMemory;
            var mem2 = db2.GetDbModules.First() as TestDbInMemory;
            var mem3 = db3.GetDbModules.First() as TestDbInMemory;

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

            inputС3.Start();
            distributor3.Start();
            netRc3.Start();
            async3.Start();

            q4.Start();

            ddistributor.UpdateModel();
            distributor.UpdateModel();
            distributor2.UpdateModel();

            distributor3.Restore(new ServerId("localhost", distrServer1), true);

            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            Assert.AreEqual(0, mem.Remote);
            Assert.AreEqual(0, mem2.Remote);
            Assert.AreEqual(0, mem3.Remote);
            Assert.AreNotEqual(0, mem3.Local);
            Assert.AreEqual(count, mem.Local + mem2.Local + mem3.Local);
            Assert.AreEqual(false, restore.IsNeedRestore);
            Assert.AreEqual(false, restore2.IsNeedRestore);
            Assert.AreEqual(false, restore3.IsNeedRestore);

            distributor.Dispose();
            distributor2.Dispose();
            distributor3.Dispose();
            ddistributor.Dispose();

            async.Dispose();
            async2.Dispose();
            async3.Dispose();

            dnet.Dispose();
            net.Dispose();
            net2.Dispose();
            net3.Dispose();

            restore.Dispose();
            restore2.Dispose();
            restore3.Dispose();
        }

        [TestMethod]
        public void Writer_Restore_TwoServersWhenOneServerNotAvailable()
        {
            const int proxyServer = 22217;
            const int distrServer1 = 22218;
            const int distrServer12 = 22219;
            const int storageServer1 = 22220;
            const int storageServer2 = 22221;

            #region hell

            var writer =
                new HashWriter(new HashMapConfiguration("TestRestoreWithServers", HashMapCreationMode.CreateNew, 2, 1,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.SetServer(1, "localhost", storageServer2, 157);
            writer.Save();

            var netconfig = new NetConfiguration("localhost", proxyServer, "testService", 10);
            var toconfig = new ProxyConfiguration(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
            var common = new CommonConfiguration(1, 100);

            var proxy = new TestGate(netconfig, toconfig, common);

            var distrNet = new DistributorNetConfiguration("localhost",
                distrServer1, distrServer12, "testService", 10);
            var distrConf = new DistributorConfiguration(1, "TestRestoreWithServers",
                TimeSpan.FromMilliseconds(10000000), TimeSpan.FromMilliseconds(500000), TimeSpan.FromMinutes(100),
                TimeSpan.FromMilliseconds(10000000));

            var distr = new DistributorApi(distrNet, distrConf, common);

            var storageNet1 = new StorageNetConfiguration("localhost", storageServer1, 157, "testService", 10);
            var storageConfig = new StorageConfiguration("TestRestoreWithServers", 1, 10,
                TimeSpan.FromMilliseconds(10000),
                TimeSpan.FromMilliseconds(200), TimeSpan.FromHours(1), TimeSpan.FromHours(1), false);

            var storage1 = new StorageApi(storageNet1, storageConfig, common);
            var storageNet2 = new StorageNetConfiguration("localhost", storageServer2, 157, "testService", 10);
            var storage2 = new StorageApi(storageNet2, storageConfig, common);

            #endregion

            proxy.Build();
            proxy.Start();

            distr.Build();
            distr.Start();

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

            proxy.Dispose();
            distr.Dispose();
            storage1.Dispose();
            storage2.Dispose();
        }

        [TestMethod]
        public void Writer_Restore_SelfRestore()
        {
            const int proxyServer = 22322;
            const int distrServer1 = 22323;
            const int distrServer12 = 22324;
            const int storageServer1 = 22325;
            const int storageServer2 = 22326;

            #region hell

            var writer =
                new HashWriter(new HashMapConfiguration("TestSelfRestore", HashMapCreationMode.CreateNew, 2, 1,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.SetServer(1, "localhost", storageServer2, 157);
            writer.Save();

            var netconfig = new NetConfiguration("localhost", proxyServer, "testService", 10);
            var toconfig = new ProxyConfiguration(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
            var common = new CommonConfiguration(1, 100);

            var proxy = new TestGate(netconfig, toconfig, common);

            var distrNet = new DistributorNetConfiguration("localhost",
                distrServer1, distrServer12, "testService", 10);
            var distrConf = new DistributorConfiguration(1, "TestSelfRestore",
                TimeSpan.FromMilliseconds(10000000), TimeSpan.FromMilliseconds(500000), TimeSpan.FromMinutes(100),
                TimeSpan.FromMilliseconds(10000000));

            var distr = new DistributorApi(distrNet, distrConf, common);


            var storageNet1 = new StorageNetConfiguration("localhost", storageServer1, 157, "testService", 10);
            var storageConfig = new StorageConfiguration("TestSelfRestore", 1, 10, TimeSpan.FromMilliseconds(10000),
                TimeSpan.FromMilliseconds(200), TimeSpan.FromHours(1), TimeSpan.FromHours(1), false);

            var factory = new TestInMemoryDbFactory();
            var storage1 = new StorageApi(storageNet1, storageConfig, common);

            #endregion

            proxy.Build();
            proxy.Start();

            distr.Build();
            distr.Start();

            proxy.Int.SayIAmHere("localhost", distrServer1);

            storage1.Build();
            storage1.AddDbModule(factory);
            storage1.Start();

            const int count = 50;

            for (int i = 0; i < count; i++)
            {
                var wait = proxy.Int.CreateSync(i, i);

                if (wait.IsError)
                    wait = proxy.Int.CreateSync(i, i);

                Assert.AreEqual(RequestState.Complete, wait.State);
            }

            Assert.AreEqual(count, factory.Db.Local + factory.Db.Remote);

            writer =
                new HashWriter(new HashMapConfiguration("TestSelfRestore", HashMapCreationMode.CreateNew, 1, 1,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.Save();

            storage1.Api.UpdateModel();
            storage1.Api.Restore(new ServerAddress("localhost", distrServer12), true);

            Thread.Sleep(1000);

            Assert.AreEqual(count, factory.Db.Local);

            proxy.Dispose();
            distr.Dispose();
            storage1.Dispose();
        }

        [TestMethod]
        public void Writer_Restore_TimeoutDelete()
        {
            const int proxyServer = 22331;
            const int distrServer1 = 22332;
            const int distrServer12 = 22333;
            const int storageServer1 = 22334;

            #region hell

            var writer =
                new HashWriter(new HashMapConfiguration("TestTimeoutDeleteRestore", HashMapCreationMode.CreateNew, 1, 1,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.Save();

            var netconfig = new NetConfiguration("localhost", proxyServer, "testService", 10);
            var toconfig = new ProxyConfiguration(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
            var common = new CommonConfiguration(1, 100);

            var proxy = new TestGate(netconfig, toconfig, common);

            var distrNet = new DistributorNetConfiguration("localhost",
                distrServer1, distrServer12, "testService", 10);
            var distrConf = new DistributorConfiguration(1, "TestTimeoutDeleteRestore",
                TimeSpan.FromMilliseconds(10000000), TimeSpan.FromMilliseconds(500000), TimeSpan.FromMinutes(100),
                TimeSpan.FromMilliseconds(10000000));

            var distr = new DistributorApi(distrNet, distrConf, common);

            var storageNet1 = new StorageNetConfiguration("localhost", storageServer1, 157, "testService", 10);
            var storageConfig = new StorageConfiguration("TestTimeoutDeleteRestore", 1, 10,
                TimeSpan.FromMilliseconds(10000),
                TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(1), TimeSpan.FromSeconds(1), false);

            var factory = new TestInMemoryDbFactory();
            var storage1 = new StorageApi(storageNet1, storageConfig, new CommonConfiguration(1, 10));

            #endregion

            proxy.Build();
            proxy.Start();

            distr.Build();
            distr.Start();

            proxy.Int.SayIAmHere("localhost", distrServer1);

            storage1.Build();
            storage1.AddDbModule(factory);
            storage1.Start();

            const int count = 50;

            for (int i = 0; i < count; i++)
            {
                var wait = proxy.Int.CreateSync(i, i);

                Assert.AreEqual(RequestState.Complete, wait.State);
            }

            Assert.AreEqual(count, factory.Db.Local);

            for (int i = 0; i < count / 2; i++)
            {
                var wait = proxy.Int.DeleteSync(i);

                Assert.AreEqual(RequestState.Complete, wait.State);
            }

            Assert.AreEqual(count / 2, factory.Db.Local);
            Assert.AreEqual(count / 2, factory.Db.Deleted);

            Thread.Sleep(4000);

            Assert.AreEqual(count / 2, factory.Db.Local);
            Assert.AreEqual(0, factory.Db.Deleted);

            proxy.Dispose();
            distr.Dispose();
            storage1.Dispose();
        }
    }
}

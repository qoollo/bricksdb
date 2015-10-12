﻿using System;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Client.Support;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.Event;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DistributorModules;
using Qoollo.Impl.DistributorModules.Caches;
using Qoollo.Impl.DistributorModules.DistributorNet;
using Qoollo.Impl.DistributorModules.Model;
using Qoollo.Impl.DistributorModules.ParallelWork;
using Qoollo.Impl.DistributorModules.Transaction;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestModules;
using Consts = Qoollo.Impl.Common.Support.Consts;
using SingleConnectionToDistributor = Qoollo.Impl.Writer.WriterNet.SingleConnectionToDistributor;

namespace Qoollo.Tests
{

    [TestClass]
    public class TestDistributorModules
    {
        [TestMethod]
        public void WriterSystemModel_GetUnavailableServers_CheckAvailableAndUnAvailableServers()
        {
            var server1 = new ServerId("local", 11010);
            var server2 = new ServerId("local", 11011);
            var server3 = new ServerId("local", 11012);

            var config = new DistributorHashConfiguration(1);

            var writer =
                new HashWriter(new HashMapConfiguration("TestDbModelGetUnavalibaleServers",
                                                        HashMapCreationMode.CreateNew, 6, 3, HashFileType.Collector));
            writer.CreateMap();
            writer.SetServer(0, server1.RemoteHost, server1.Port, 157);
            writer.SetServer(1, server2.RemoteHost, server2.Port, 157);
            writer.SetServer(2, server3.RemoteHost, server3.Port, 157);
            writer.SetServer(3, server1.RemoteHost, server1.Port, 157);
            writer.SetServer(4, server2.RemoteHost, server2.Port, 157);
            writer.SetServer(5, server3.RemoteHost, server3.Port, 157);
            writer.Save();

            var model = new WriterSystemModel(config,
                                                  new HashMapConfiguration("TestDbModelGetUnavalibaleServers",
                                                                           HashMapCreationMode.ReadFromFile, 1,
                                                                           1, HashFileType.Writer));
            model.Start();

            model.ServerNotAvailable(server1);
            Assert.AreEqual(1, model.GetUnavailableServers().Count);
            model.ServerNotAvailable(server1);
            Assert.AreEqual(1, model.GetUnavailableServers().Count);
            model.ServerNotAvailable(server2);
            Assert.AreEqual(2, model.GetUnavailableServers().Count);
            model.ServerNotAvailable(server3);
            Assert.AreEqual(3, model.GetUnavailableServers().Count);
            model.ServerAvailable(server1);
            Assert.AreEqual(2, model.GetUnavailableServers().Count);
            model.ServerAvailable(server1);
            Assert.AreEqual(2, model.GetUnavailableServers().Count);
            model.ServerAvailable(server2);
            Assert.AreEqual(1, model.GetUnavailableServers().Count);
            model.ServerAvailable(server3);
            Assert.AreEqual(0, model.GetUnavailableServers().Count);
        }

        [TestMethod]
        public void MainLogicModule_TransactionAnswerResult_ReceiveAnswersFromWriter()
        {
            const int distrServer1 = 22168;
            const int distrServer2 = 23168;

            #region hell

            var connectionConf = new ConnectionConfiguration("testService", 10);

            var distrconfig = new DistributorHashConfiguration(1);
            var queueconfig = new QueueConfiguration(1, 100);
            var dnet = new DistributorNetModule(connectionConf,
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            var ddistributor = new DistributorModule(new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)), distrconfig,
                queueconfig, dnet,
                new ServerId("localhost", distrServer1),
                new ServerId("localhost", distrServer2),
                new HashMapConfiguration("TestDistributorReceiveAndDbSendAsync",
                    HashMapCreationMode.ReadFromFile,
                    1, 1, HashFileType.Distributor));
            dnet.SetDistributor(ddistributor);

            var cache =
                new DistributorTimeoutCache(new DistributorCacheConfiguration(TimeSpan.FromMilliseconds(2000),
                    TimeSpan.FromMilliseconds(200000)));
            var tranc = new TransactionModule(dnet, new TransactionConfiguration(4),
                                              distrconfig.CountReplics, cache);
            var main = new MainLogicModule(ddistributor, tranc, cache);

            var netReceive4 = new NetReceiverConfiguration(distrServer1, "localhost", "testService");
            var netReceive42 = new NetReceiverConfiguration(distrServer2, "localhost", "testService");
            var input = new InputModuleWithParallel(new QueueConfiguration(2, 100000), main, tranc);
            var receiver4 = new NetDistributorReceiver(main, input,
                                                       ddistributor, netReceive4, netReceive42);

            ddistributor.Start();
            receiver4.Start();

            #endregion

            var t = 0;
            GlobalQueue.Queue.TransactionQueue.Registrate(data => Interlocked.Increment(ref t));
            GlobalQueue.Queue.Start();

            var connection = new SingleConnectionToDistributor(
                    new ServerId("localhost", distrServer1), new ConnectionConfiguration("testService", 10),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            connection.Connect();

            connection.TransactionAnswerResult(new Transaction("123", "123"));
            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            connection.TransactionAnswerResult(new Transaction("1243", "1423"));
            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            Assert.AreEqual(2, t);

            connection.Dispose();
            receiver4.Dispose();
        }

        [TestMethod]
        public void TransactionModule_ProcessSyncWithExecutor_NoServersToSendData()
        {
            var s1 = new TestServerDescription(1);
            var s2 = new TestServerDescription(2);            

            var cache = new DistributorTimeoutCache(new DistributorCacheConfiguration(TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(1)));

            var net = new NetModuleTest(new Dictionary<ServerId, bool> { { s1, false }, { s2, true } });
            var trm = new TransactionModule(net, new TransactionConfiguration(1), 2, cache);

            trm.Start();

            GlobalQueue.Queue.Start();

            var data = new InnerData(new Transaction("123", ""))
            {
                Transaction =
                {
                    OperationName = OperationName.Create
                },
                DistributorData = new DistributorData { Destination = new List<ServerId> { s1, s2 } }
            };
            cache.AddDataToCache(data);

            using (var trans = trm.Rent())
            {
                trm.ProcessWithExecutor(data, trans.Element);
            }

            Thread.Sleep(1000);

            Assert.IsTrue(data.Transaction.IsError);
            trm.Dispose();
        }

        [TestMethod]
        public void TransactionModule_ProcessSyncWithExecutor_SuccessSendDataToServers()
        {
            var server1 = new ServerId("localhost", 21131);
            var server2 = new ServerId("localhost", 21132);

            var netconfig = new ConnectionConfiguration("testService", 10);
            var queueconfig = new QueueConfiguration(1, 100);
            var distrconfig = new DistributorHashConfiguration(2);
            var distributor = new DistributorModule(new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)), distrconfig,
                queueconfig, null, new ServerId("localhost", 1),
                new ServerId("localhost", 1),
                new HashMapConfiguration("TestTransactionModule",
                    HashMapCreationMode.CreateNew, 1, 1, HashFileType.Distributor));

            var net = new DistributorNetModule(netconfig,
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            net.SetDistributor(distributor);
            distributor.Start();
            net.Start();

            var s1 = TestHelper.OpenWriterHost(server1, netconfig);
            var s2 = TestHelper.OpenWriterHost(server2, netconfig);

            net.ConnectToWriter(server1);
            net.ConnectToWriter(server2);
            Thread.Sleep(TimeSpan.FromMilliseconds(1000));
            var ev = new InnerData(new Transaction("", ""))
            {
                DistributorData = new DistributorData { Destination = new List<ServerId> { server1, server2 } },
            };

            var trm = new TransactionModule(net, new TransactionConfiguration(1), 2,
                new DistributorTimeoutCache(new DistributorCacheConfiguration(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1))));
            trm.Start();

            using (var trans = trm.Rent())
            {
                trm.ProcessWithExecutor(ev, trans.Element);
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(100));

            Assert.AreEqual(1, s1.Value);
            Assert.AreEqual(1, s2.Value);
            Assert.IsFalse(ev.Transaction.IsError);

            net.Dispose();
            distributor.Dispose();
            trm.Dispose();
        }

        [TestMethod]
        public void TransactionModule_ProcessSyncWithExecutor_RollbackNoEnoughServers()
        {
            var server1 = new ServerId("localhost", 21141);
            var server2 = new ServerId("localhost", 21142);
            var server3 = new ServerId("localhost", 21143);

            #region hell

            var netconfig = new ConnectionConfiguration("testService", 10);
            var queueconfig = new QueueConfiguration(1, 100);
            var distrconfig = new DistributorHashConfiguration(2);
            var distributor = new DistributorModule(new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)), distrconfig,
                queueconfig, null, new ServerId("localhost", 1),
                new ServerId("localhost", 1),
                new HashMapConfiguration("test10", HashMapCreationMode.CreateNew, 1, 1, HashFileType.Distributor));

            var net = new DistributorNetModule(netconfig,
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            net.SetDistributor(distributor);
            distributor.Start();
            net.Start();
            
            var cache = new DistributorTimeoutCache(
                new DistributorCacheConfiguration(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1)));

            var trm = new TransactionModule(net, new TransactionConfiguration(1), 3, cache);
            trm.Start();

            GlobalQueue.Queue.Start();

            var s1 = TestHelper.OpenWriterHost(server1, netconfig);
            var s2 = TestHelper.OpenWriterHost(server2, netconfig);

            net.ConnectToWriter(server1);
            net.ConnectToWriter(server2);

            #endregion

            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            var data = new InnerData(new Transaction("123", ""))
            {
                Transaction = {OperationName = OperationName.Create},
                DistributorData = new DistributorData {Destination = new List<ServerId> {server1, server2, server3},}
            };
            cache.AddDataToCache(data);
            
            using (var trans = trm.Rent())
            {
                trm.ProcessWithExecutor(data, trans.Element);
            }

            Thread.Sleep(2000);

            Assert.IsTrue(s1.Value <= 0);
            Assert.IsTrue(s2.Value <= 0);
            Assert.IsTrue(data.Transaction.IsError);

            net.Dispose();   
            trm.Dispose();
        }

        [TestMethod]
        public void NetModule_Process_SendDatatoAvaliableAndUnavalilableServers()
        {
            var server1 = new ServerId("localhost", 21121);
            var server2 = new ServerId("localhost", 21122);
            var server3 = new ServerId("localhost", 21123);
            var netconfig = new ConnectionConfiguration("testService", 10);
            var queueconfig = new QueueConfiguration(1, 100);
            var distrconfig = new DistributorHashConfiguration(2);

            var s1 = TestHelper.OpenWriterHost(server1, netconfig);
            var s2 = TestHelper.OpenWriterHost(server2, netconfig);

            var net = new DistributorNetModule(netconfig,
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            var distributor = new DistributorModule(new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)), distrconfig,
                queueconfig, net, new ServerId("localhost", 1),
                new ServerId("localhost", 1),
                new HashMapConfiguration("test12", HashMapCreationMode.CreateNew, 1,
                    1, HashFileType.Distributor));
            net.SetDistributor(distributor);
            distributor.Start();
            net.Start();
            GlobalQueue.Queue.Start();

            net.ConnectToWriter(server1);
            net.ConnectToWriter(server2);

            var ev = new InnerData(new Transaction("", ""))
            {
                DistributorData = new DistributorData { Destination = new List<ServerId> { server1 } },
            };

            var ret1 = net.Process(server1, ev);
            var ret2 = net.Process(server2, ev);
            var ret3 = net.Process(server3, ev);
            Assert.AreEqual(1, s1.Value);
            Assert.AreEqual(1, s2.Value);
            Assert.AreEqual(typeof(SuccessResult), ret1.GetType());
            Assert.AreEqual(typeof(SuccessResult), ret2.GetType());
            Assert.AreEqual(typeof(ServerNotFoundResult), ret3.GetType());

            GlobalQueue.Queue.Dispose();
            net.Dispose();
        }

        [TestMethod]
        public void DistributorTimeoutCache_GetUpdate()
        {
            var cache = new DistributorTimeoutCache(
                new DistributorCacheConfiguration(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(500)));

            var ev = new InnerData(new Transaction("123", ""))
            {
                DistributorData = new DistributorData { Destination = new List<ServerId>() },
            };

            cache.AddToCache("123", ev);
            var ret = cache.Get("123");
            Assert.AreEqual(ev, ret);
            ev.Transaction.Complete();
            cache.Update("123", ev);
            ret = cache.Get("123");
            Assert.AreEqual(ev, ret);
            Thread.Sleep(200);
            ret = cache.Get("123");
            Assert.AreEqual(ev, ret);
            Assert.AreEqual(TransactionState.Complete, ev.Transaction.State);
            Thread.Sleep(500);
            ret = cache.Get("123");
            Assert.AreEqual(null, ret);
        }

        [TestMethod]
        public void DistributorTimeoutCache_TimeoutData_SendToMainLogicModuleObsoleteData()
        {
            var cache = new DistributorTimeoutCache(
                new DistributorCacheConfiguration(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(500)));

            var net = new DistributorNetModule(new ConnectionConfiguration("", 1),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            var trans = new TransactionModule(net, new TransactionConfiguration(1), 1, cache);

            var ev = new InnerData(new Transaction("123", "") { OperationName = OperationName.Create })
            {
                DistributorData = new DistributorData { Destination = new List<ServerId>() },
            };

            ev.Transaction.Complete();
            cache.AddToCache(ev.Transaction.CacheKey, ev);
            var ret = cache.Get(ev.Transaction.CacheKey);
            Assert.AreEqual(ev, ret);
            Thread.Sleep(200);
            cache.Get(ev.Transaction.CacheKey);
            Assert.AreEqual(TransactionState.Complete, ev.Transaction.State);
            Thread.Sleep(200);
            cache.Get(ev.Transaction.CacheKey);
            Assert.AreEqual(TransactionState.Complete, ev.Transaction.State);
            Thread.Sleep(500);
            ret = cache.Get(ev.Transaction.CacheKey);
            Assert.AreEqual(null, ret);

            ev = new InnerData(new Transaction("1231", "") { OperationName = OperationName.Create })
            {
                DistributorData = new DistributorData { Destination = new List<ServerId>() },
            };

            ev.Transaction.StartTransaction();
            cache.AddToCache(ev.Transaction.CacheKey, ev);
            ret = cache.Get(ev.Transaction.CacheKey);
            Assert.AreEqual(ev, ret);
            Thread.Sleep(200);
            cache.Get(ev.Transaction.CacheKey);
            Assert.AreEqual(TransactionState.Error, ev.Transaction.State);
            Thread.Sleep(200);
            cache.Get(ev.Transaction.CacheKey);
            Assert.AreEqual(TransactionState.Error, ev.Transaction.State);
            Thread.Sleep(500);
            ret = cache.Get(ev.Transaction.CacheKey);
            Assert.AreEqual(null, ret);
        }

        [TestMethod]
        public void WriterSystemModel_GetDestination_ChechAvailableServers()
        {
            var config = new DistributorHashConfiguration(1);

            var writer = new HashWriter(new HashMapConfiguration("test", HashMapCreationMode.CreateNew, 6, 3, HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "local", 11010, 157);
            writer.SetServer(1, "local", 11011, 157);
            writer.SetServer(2, "local", 11012, 157);
            writer.SetServer(3, "local", 11010, 157);
            writer.SetServer(4, "local", 11011, 157);
            writer.SetServer(5, "local", 11012, 157);
            writer.Save();

            var model = new WriterSystemModel(config,
                                                  new HashMapConfiguration("test", HashMapCreationMode.ReadFromFile, 1,
                                                                           1, HashFileType.Distributor));
            model.Start();

            var ev = new InnerData(new Transaction("123", ""))
            {
                Transaction = new Transaction(HashConvertor.GetString("1"), ""),
                DistributorData = new DistributorData { Destination = new List<ServerId>() },
            };

            var ret = model.GetDestination(ev);
            Assert.IsTrue(ret.Count == 1);
            model.ServerNotAvailable(ret.First());
            var ret2 = model.GetDestination(ev);
            Assert.IsTrue(ret2.Count == 1);
            Assert.AreNotEqual(ret.First(), ret2.First());
            model.ServerNotAvailable(ret2.First());
            var ret3 = model.GetDestination(ev);
            Assert.IsTrue(ret3.Count == 1);
            Assert.AreNotEqual(ret.First(), ret3.First());
            Assert.AreNotEqual(ret3.First(), ret2.First());
            model.ServerNotAvailable(ret3.First());
            var ret4 = model.GetDestination(ev);
            Assert.IsTrue(ret4.Count == 0);
        }

     
        [TestMethod]
        public void MainLogic_ProcessWithData_SendAllReplicsThenObsoleteDataInCache()
        {
            var writer = new HashWriter(new HashMapConfiguration("test9", HashMapCreationMode.CreateNew, 2, 3, HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", 21111, 157);
            writer.SetServer(1, "localhost", 21112, 157);
            writer.Save();

            var cache = new DistributorTimeoutCache(
                new DistributorCacheConfiguration(TimeSpan.FromMilliseconds(400), TimeSpan.FromMilliseconds(1000)));
            var distrconfig = new DistributorHashConfiguration(2);
            var queueconfig = new QueueConfiguration(1, 100);
            var netconfig = new ConnectionConfiguration("testService", 10);
            var net = new DistributorNetModule(netconfig,
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            var distributor = new DistributorModule(new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)), distrconfig,
                queueconfig, net, new ServerId("localhost", 1),
                new ServerId("localhost", 1),
                new HashMapConfiguration("test9", HashMapCreationMode.ReadFromFile,
                    1, 1, HashFileType.Distributor));
            net.SetDistributor(distributor);
            var transaction = new TransactionModule(net, new TransactionConfiguration(1),
                                                    distrconfig.CountReplics, cache);
            var main = new MainLogicModule(distributor, transaction, cache);

            var server1 = new ServerId("localhost", 21111);
            var server2 = new ServerId("localhost", 21112);

            var s1 = TestHelper.OpenWriterHost(server1, netconfig);
            var s2 = TestHelper.OpenWriterHost(server2, netconfig);

            cache.Start();
            distributor.Start();
            net.Start();
            transaction.Start();
            main.Start();

            GlobalQueue.Queue.Start();

            net.ConnectToWriter(server1);
            net.ConnectToWriter(server2);

            Thread.Sleep(TimeSpan.FromMilliseconds(300));
            var ev = new InnerData(new Transaction("123", "default"))
            {
                Transaction =
                    new Transaction(HashConvertor.GetString("1"), "default")
                    {
                        OperationName = OperationName.Create,
                        OperationType = OperationType.Async
                    },
                DistributorData = new DistributorData { Destination = new List<ServerId>() },
            };            

            using (var trans = transaction.Rent())
            {
                main.ProcessWithData(ev, trans.Element);
            }

            GlobalQueue.Queue.TransactionQueue.Add(ev.Transaction);
            GlobalQueue.Queue.TransactionQueue.Add(ev.Transaction);
            Thread.Sleep(TimeSpan.FromMilliseconds(300));

            Assert.IsTrue(s1.Value > 0);
            Assert.IsTrue(s2.Value > 0);
            Assert.AreEqual(main.GetTransactionState(ev.Transaction.UserTransaction).State, TransactionState.Complete);
            Thread.Sleep(TimeSpan.FromMilliseconds(1000));
            Assert.AreEqual(main.GetTransactionState(ev.Transaction.UserTransaction).State, TransactionState.DontExist);

            net.Dispose();
            distributor.Dispose();
            transaction.Dispose();
            main.Dispose();
            GlobalQueue.Queue.Dispose();
        }

        [TestMethod]
        public void WriterSystemModel_GetDestination_CountReplics()
        {
            var config = new DistributorHashConfiguration(4);

            var writer = new HashWriter(new HashMapConfiguration("testhash", HashMapCreationMode.CreateNew, 6, 3, HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "local", 11010, 157);
            writer.SetServer(1, "local", 11011, 157);
            writer.SetServer(2, "local", 11012, 157);
            writer.SetServer(3, "local", 11010, 157);
            writer.SetServer(4, "local", 11011, 157);
            writer.SetServer(5, "local", 11012, 157);
            writer.Save();

            var model = new WriterSystemModel(config,
                                                  new HashMapConfiguration("testhash", HashMapCreationMode.ReadFromFile,
                                                                           1, 1, HashFileType.Distributor));
            model.Start();

            var ev = new InnerData(new Transaction("123", ""))
            {
                Transaction = new Transaction(HashConvertor.GetString("1"), ""),
                DistributorData = new DistributorData { Destination = new List<ServerId>() },
            };            

            var ret = model.GetDestination(ev);
            Assert.IsTrue(ret.Count == 0);
            model = new WriterSystemModel(new DistributorHashConfiguration(3),
                                              new HashMapConfiguration("testhash", HashMapCreationMode.ReadFromFile, 1,
                                                                       1, HashFileType.Distributor));
            model.Start();

            ret = model.GetDestination(ev);
            Assert.AreEqual(3, ret.Count);
            Assert.AreNotEqual(ret[0], ret[1]);
            Assert.AreNotEqual(ret[0], ret[2]);
            Assert.AreNotEqual(ret[2], ret[1]);
        }

        [TestMethod]
        public void InputModuleWithParallel_ProcessAsync_SendToOneServers_Success()
        {
            const int distrServer1 = 22161;
            const int distrServer2 = 23161;
            const int storageServer1 = 22162;

            var q1 = new GlobalQueueInner();

            var writer = new HashWriter(new HashMapConfiguration("test13", HashMapCreationMode.CreateNew, 1, 1, HashFileType.Distributor));
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
                new ServerId("localhost", distrServer2),
                new HashMapConfiguration("test13", HashMapCreationMode.ReadFromFile,
                    1, 1, HashFileType.Distributor));
            dnet.SetDistributor(ddistributor);

            var cache = new DistributorTimeoutCache(
                new DistributorCacheConfiguration(TimeSpan.FromSeconds(200), TimeSpan.FromSeconds(200)));
            var tranc = new TransactionModule(dnet, new TransactionConfiguration(4),
                distrconfig.CountReplics, cache);

            var main = new MainLogicModule(ddistributor, tranc, cache);            

            var netReceive4 = new NetReceiverConfiguration(distrServer1, "localhost", "testService");
            var netReceive42 = new NetReceiverConfiguration(distrServer2, "localhost", "testService");
            var input = new InputModuleWithParallel(new QueueConfiguration(2, 100000), main, tranc);
            var receiver4 = new NetDistributorReceiver(main, input, ddistributor, netReceive4, netReceive42);

            #endregion

            var s = TestHelper.OpenWriterHost(new ServerId("localhost", storageServer1),
                                       new ConnectionConfiguration("testService", 10));

            tranc.Start();
            main.Start();
            receiver4.Start();
            input.Start();
            dnet.Start();
            ddistributor.Start();

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
                        Data = CommonDataSerializer.Serialize(i)
                    };
                list.Add(ev);
            }

            foreach (var data in list)
            {
                input.ProcessAsync(data);
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            Assert.AreEqual(count, s.Value);

            foreach (var data in list)
            {
                var transaction = main.GetTransactionState(data.Transaction.UserTransaction);
                Assert.AreEqual(TransactionState.TransactionInProcess, transaction.State);
            }
            foreach (var data in list)
            {
                q1.TransactionQueue.Add(data.Transaction);
            }
            Thread.Sleep(TimeSpan.FromMilliseconds(1000));
            foreach (var data in list)
            {
                var transaction = main.GetTransactionState(data.Transaction.UserTransaction);
                Assert.AreEqual(TransactionState.Complete, transaction.State);
            }
            q1.Dispose();

            ddistributor.Dispose();
            dnet.Dispose();
            cache.Dispose();
            tranc.Dispose();
        }

        [TestMethod]
        public void InputModuleWithParallel_ProcessAsync_SendToTwoServers_Success()
        {
            const int distrServer1 = 22163;
            const int distrServer2 = 23163;
            const int storageServer1 = 22164;
            const int storageServer2 = 22165;

            var q1 = new GlobalQueueInner();

            var writer = new HashWriter(new HashMapConfiguration("test14", HashMapCreationMode.CreateNew, 2, 2, HashFileType.Distributor));
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
                new ServerId("localhost", distrServer2),
                new HashMapConfiguration("test14", HashMapCreationMode.ReadFromFile,
                    1, 2, HashFileType.Distributor));
            dnet.SetDistributor(ddistributor);

            var cache = new DistributorTimeoutCache(
                new DistributorCacheConfiguration(TimeSpan.FromSeconds(200), TimeSpan.FromSeconds(200)));
            var tranc = new TransactionModule(dnet, new TransactionConfiguration(4),
                                              distrconfig.CountReplics, cache);
            var main = new MainLogicModule(ddistributor, tranc, cache);

            var netReceive4 = new NetReceiverConfiguration(distrServer1, "localhost", "testService");
            var netReceive42 = new NetReceiverConfiguration(distrServer2, "localhost", "testService");
            var input = new InputModuleWithParallel(new QueueConfiguration(2, 100000), main, tranc);
            var receiver4 = new NetDistributorReceiver(main, input, ddistributor, netReceive4, netReceive42);

            #endregion

            var s1 = TestHelper.OpenWriterHost(new ServerId("localhost", storageServer1),
                                        new ConnectionConfiguration("testService", 10));

            var s2 = TestHelper.OpenWriterHost(new ServerId("localhost", storageServer2),
                                        new ConnectionConfiguration("testService", 10));

            tranc.Start();
            main.Start();
            receiver4.Start();
            input.Start();
            dnet.Start();
            ddistributor.Start();

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
                        Data = CommonDataSerializer.Serialize(i)
                    };
                list.Add(ev);
            }

            foreach (var data in list)
            {
                input.ProcessAsync(data);
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            Assert.AreEqual(count, s1.Value);
            Assert.AreEqual(count, s2.Value);

            foreach (var data in list)
            {
                var transaction = main.GetTransactionState(data.Transaction.UserTransaction);
                Assert.AreEqual(TransactionState.TransactionInProcess, transaction.State);
            }
            foreach (var data in list)
            {
                q1.TransactionQueue.Add(data.Transaction);
            }
            foreach (var data in list)
            {
                q1.TransactionQueue.Add(data.Transaction);
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(1000));
            foreach (var data in list)
            {
                var transaction = main.GetTransactionState(data.Transaction.UserTransaction);
                Assert.AreEqual(TransactionState.Complete, transaction.State);
            }
            q1.Dispose();

            ddistributor.Dispose();
            dnet.Dispose();
            cache.Dispose();
            input.Dispose();
            tranc.Dispose();
        }

        [TestMethod]
        public void InputModuleWithParallel_ProcessAsync_SendToOneServersAndTimeoutInCache_Success()
        {
            const int distrServer1 = 22166;
            const int distrServer2 = 23166;
            const int storageServer1 = 22167;

            var q1 = new GlobalQueueInner();

            var writer =
                new HashWriter(new HashMapConfiguration("testAsyncTrans1S", HashMapCreationMode.CreateNew, 1, 1, HashFileType.Distributor));
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
                new ServerId("localhost", distrServer2),
                new HashMapConfiguration("testAsyncTrans1S",
                    HashMapCreationMode.ReadFromFile,
                    1, 1, HashFileType.Distributor));
            dnet.SetDistributor(ddistributor);

            var cache = new DistributorTimeoutCache(
                new DistributorCacheConfiguration(TimeSpan.FromMilliseconds(2000), TimeSpan.FromMilliseconds(200000)));
            var tranc = new TransactionModule(dnet, new TransactionConfiguration(4),
                                              distrconfig.CountReplics, cache);
            var main = new MainLogicModule(ddistributor, tranc, cache);

            var netReceive4 = new NetReceiverConfiguration(distrServer1, "localhost", "testService");
            var netReceive42 = new NetReceiverConfiguration(distrServer2, "localhost", "testService");
            var input = new InputModuleWithParallel(new QueueConfiguration(2, 100000), main, tranc);
            var receiver4 = new NetDistributorReceiver(main, input, ddistributor, netReceive4, netReceive42);

            #endregion

            var s = TestHelper.OpenWriterHost(new ServerId("localhost", storageServer1),
                                       new ConnectionConfiguration("testService", 10));

            main.Start();
            receiver4.Start();
            input.Start();
            dnet.Start();
            ddistributor.Start();

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
                        Data = CommonDataSerializer.Serialize(i)
                    };
                list.Add(ev);
            }

            foreach (var data in list)
            {
                input.ProcessAsync(data);
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(1000));

            Assert.AreEqual(count, s.Value);

            foreach (var data in list)
            {
                var transaction = main.GetTransactionState(data.Transaction.UserTransaction);
                Assert.AreEqual(TransactionState.TransactionInProcess, transaction.State);
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(2000));
            foreach (var data in list)
            {
                var transaction = main.GetTransactionState(data.Transaction.UserTransaction);
                if (transaction.State == TransactionState.DontExist)
                    Thread.Sleep(1000);
                transaction = main.GetTransactionState(data.Transaction.UserTransaction);
                Assert.AreEqual(TransactionState.Error, transaction.State);
            }

            q1.Dispose();

            ddistributor.Dispose();
            dnet.Dispose();
            cache.Dispose();
        }        
    }
}

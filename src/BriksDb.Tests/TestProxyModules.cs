﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.Event;
using Qoollo.Impl.Common.NetResults.System.Distributor;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DistributorModules;
using Qoollo.Impl.DistributorModules.Caches;
using Qoollo.Impl.DistributorModules.DistributorNet;
using Qoollo.Impl.DistributorModules.ParallelWork;
using Qoollo.Impl.DistributorModules.Transaction;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.Proxy;
using Qoollo.Impl.Proxy.Caches;
using Qoollo.Impl.Proxy.Model;
using Qoollo.Impl.Proxy.ProxyNet;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestProxy;
using Xunit;
using Assert = Xunit.Assert;

namespace Qoollo.Tests
{
    [Collection("test collection 1")]
    public class TestProxyModules:TestBase
    {
        [Fact]
        public void ProxySystem_CreateSync_SendSyncDataToFakeDistributor_NoError()
        {
            var queue = new QueueConfiguration(1, 100);
            var connection = new ConnectionConfiguration("testService", 10);
            var ndrc2 = new NetReceiverConfiguration(32190, "localhost", "testService");
            var pcc = new ProxyCacheConfiguration(TimeSpan.FromSeconds(20000000));

            var proxy = new TestProxySystem(new ServerId("", 1), queue, connection, pcc, pcc, ndrc2,
                new AsyncTasksConfiguration(TimeSpan.FromMinutes(60000)),
                new AsyncTasksConfiguration(TimeSpan.FromMinutes(60000)),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));

            proxy.Build();
            proxy.Start();

            var distr = new ServerId("localhost", 22190);
            TestHelper.OpenDistributorHost(distr, connection);
            proxy.Distributor.SayIAmHere(distr);

            var provider = new StoredDataHashCalculator();

            var hash = provider.CalculateHashFromKey(10);
            var transaction = new Transaction(hash, "")
            {
                OperationName = OperationName.Create,
                OperationType = OperationType.Sync
            };

            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(200);
                transaction.Complete();
                proxy.Queue.ProxyDistributorQueue.Add(new OperationCompleteCommand(transaction));
            });

            var api = proxy.CreateApi("", false, provider);
            var wait = api.CreateSync(10, TestHelper.CreateStoredData(10));
            wait.Wait();
            Assert.Equal(TransactionState.Complete, wait.Result.State);

            proxy.Dispose();
        }

        [Fact]
        public void ProxySystem_Read_ReadFromFakeDistributor_ExpectedData()
        {
            var queue = new QueueConfiguration(1, 100);
            var connection = new ConnectionConfiguration("testService", 10);
            var ndrc2 = new NetReceiverConfiguration(32192, "localhost", "testService");
            var pcc = new ProxyCacheConfiguration(TimeSpan.FromSeconds(20000000));

            var proxy = new TestProxySystem(new ServerId("", 1), queue, connection, pcc, pcc,
                ndrc2,
                new AsyncTasksConfiguration(TimeSpan.FromMinutes(60000)),
                new AsyncTasksConfiguration(TimeSpan.FromMinutes(60000)),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));

            proxy.Build();
            proxy.Start();

            var distr = new ServerId("localhost", 22194);
            TestHelper.OpenDistributorHost(distr, connection);
            proxy.Distributor.SayIAmHere(distr);

            var provider = new IntHashConvertor();

            var hash = provider.CalculateHashFromKey(10);
            var transaction = new Transaction(hash, "")
            {
                OperationName = OperationName.Read,
                OperationType = OperationType.Sync
            };

            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(200);
                transaction.Complete();
                var data = new InnerData(transaction)
                {
                    Data = provider.SerializeValue(10)
                };
                proxy.Queue.ProxyDistributorQueue.Add(new ReadOperationCompleteCommand(data));
            });

            UserTransaction userTransaction;
            var api = proxy.CreateApi("", false, new IntHashConvertor());
            var wait = api.Read(10, out userTransaction);

            Assert.Equal(10, wait);
            Assert.Equal(TransactionState.Complete, userTransaction.State);

            proxy.Dispose();
        }

        [Fact]
        public void AsyncProxyCache_AddToCache_WaitRemovedCallback_ExpiredData()
        {
            var cache = new AsyncProxyCache(TimeSpan.FromMilliseconds(200));
            var ev = new InnerData(new Transaction("123", ""))
            {
                Transaction = { UserSupportCallback = new TaskCompletionSource<UserTransaction>() }
            };
            Task<UserTransaction> wait = ev.Transaction.UserSupportCallback.Task;

            cache.AddToCache("123", ev.Transaction);

            wait.Wait();
            Assert.True(wait.Result.IsError);

            cache.Dispose();
        }

        [Fact]
        public void ProxyNetModule_Process_SendDataToDistributors_2SuccessAnd1Fail()
        {
            var server1 = new ServerId("localhost", 21161);
            var server2 = new ServerId("localhost", 21162);
            var server3 = new ServerId("localhost", 21163);
            var netconfig = new ConnectionConfiguration("testService", 1);

            var s1 = TestHelper.OpenDistributorHost(server1, netconfig);
            var s2 = TestHelper.OpenDistributorHost(server2, netconfig);

            var net = new ProxyNetModule(netconfig,
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));

            var distr = new TestProxyDistributorModule();
            net.SetDistributor(distr);
            net.Start();

            net.ConnectToDistributor(server1);
            net.ConnectToDistributor(server2);

            var ev = new InnerData(new Transaction("", ""))
            {
                DistributorData = new DistributorData { Destination = new List<ServerId> { server1 } },
            };

            var ret1 = net.Process(server1, ev);
            var ret2 = net.Process(server2, ev);
            var ret3 = net.Process(server3, ev);

            Thread.Sleep(TimeSpan.FromMilliseconds(200));
            Assert.Equal(1, s1.Value);
            Assert.Equal(1, s2.Value);
            Assert.Equal(typeof(SuccessResult), ret1.GetType());
            Assert.Equal(typeof(SuccessResult), ret2.GetType());
            Assert.Equal(typeof(ServerNotFoundResult), ret3.GetType());

            net.Dispose();
            distr.Dispose();
            s1.Dispose();
            s2.Dispose();
        }

        [Fact]
        public void ProxyDistributorModule_TransactionDestination_CreateTransAndGetDestination()
        {
            var model = new DistributorSystemModel();
            var server1 = new ServerId("localhost", 1);
            var server2 = new ServerId("localhost", 2);

            model.AddServer(server1);
            model.AddServer(server2);

            const string hash = "123";

            var res1 = model.CreateTransaction(hash);
            Assert.NotNull(res1);
            var res2 = model.GetDestination(res1.UserTransaction);
            Assert.True(server1.Equals(res2));

            var res3 = model.CreateTransaction(hash);
            Assert.NotNull(res3);
            var res4 = model.GetDestination(res3.UserTransaction);
            Assert.True(server2.Equals(res4));

            model.ServerNotAvailable(server1);

            var res5 = model.CreateTransaction(hash);
            Assert.NotNull(res5);
            var res6 = model.GetDestination(res5.UserTransaction);
            Assert.True(server2.Equals(res6));

            model.ServerNotAvailable(server2);

            var res7 = model.CreateTransaction(hash);
            Assert.Equal(Errors.NotAvailableServersInSystem + "; ", res7.ErrorDescription);
        }

        [Fact]
        public void ProxyMainLogic_Process_SendDataToRealDistributor()
        {
            var queue = new QueueConfiguration(1, 1000);

            var server1 = new ServerId("localhost", 21171);
            var server2 = new ServerId("localhost", 21172);
            var server3 = new ServerId("localhost", 21173);
            var netconfig = new ConnectionConfiguration("testService", 10);

            var s1 = TestHelper.OpenDistributorHost(server1, netconfig);
            var s2 = TestHelper.OpenDistributorHost(server2, netconfig);

            var net = new ProxyNetModule(netconfig,
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            var cache = new ProxyCache(TimeSpan.FromSeconds(20));
            var acache = new AsyncProxyCache(TimeSpan.FromMinutes(100));
            var distributor = new ProxyDistributorModule(acache, net, queue, server1,
                                                         new AsyncTasksConfiguration(TimeSpan.FromDays(1)),
                                                         new AsyncTasksConfiguration(TimeSpan.FromDays(1)));
            net.SetDistributor(distributor);

            var main = new ProxyMainLogicModule(distributor, net, cache);

            net.Start();

            distributor.Start();

            distributor.SayIAmHere(server1);
            distributor.SayIAmHere(server2);
            distributor.SayIAmHere(server3);

            cache.Start();
            main.Start();

            const string hash = "";
            var ev = new InnerData(new Transaction("", ""));

            ev.Transaction = distributor.CreateTransaction(hash);
            ev.Transaction = distributor.CreateTransaction(hash);
            ev.Transaction = distributor.CreateTransaction(hash);
            
            ev.DistributorData = new DistributorData { Destination = new List<ServerId> { server1 } };

            bool res = main.Process(ev);

            var server = cache.Get(ev.Transaction.DataHash);
            Assert.Null(server);
            Assert.True(res);

            main.Dispose();
            distributor.Dispose();
            net.Dispose();
            cache.Dispose();
            acache.Dispose();
        }

        [Fact]
        public void ProxyDistributorModule_SayIAmHere_AddDistributor()
        {
            const int server1 = 22250;
            const int server2 = 22251;
            const int server3 = 22252;
            const int server4 = 22253;
            const int server42 = 23253;
            const int server5 = 22254;
            const int server52 = 23254;

            var filename1 = nameof(ProxyDistributorModule_SayIAmHere_AddDistributor)+"1";
            var filename2 = nameof(ProxyDistributorModule_SayIAmHere_AddDistributor)+"2";
            using (new FileCleaner(filename1))
            using (new FileCleaner(filename2))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                var q1 = new GlobalQueueInner();
                var q2 = new GlobalQueueInner();
                var q3 = new GlobalQueueInner();

                GlobalQueue.SetQueue(q1);
                var queue = new QueueConfiguration(1, 1000);
                var netconfig = new ConnectionConfiguration("testService", 10);

                var netReceive = new NetReceiverConfiguration(server1, "localhost", "testService");
                var net = new ProxyNetModule(netconfig,
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
                var distributor = new ProxyDistributorModule(new AsyncProxyCache(TimeSpan.FromMinutes(100)), net, queue, new ServerId("localhost", server1),
                                                             new AsyncTasksConfiguration(TimeSpan.FromDays(1)),
                                                             new AsyncTasksConfiguration(TimeSpan.FromDays(1)));
                net.SetDistributor(distributor);
                var receive = new ProxyNetReceiver(distributor, netReceive);

                GlobalQueue.SetQueue(q2);

                var netReceive2 = new NetReceiverConfiguration(server2, "localhost", "testService");
                var net2 = new ProxyNetModule(netconfig,
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
                var distributor2 = new ProxyDistributorModule(new AsyncProxyCache(TimeSpan.FromMinutes(100)), net2, queue, new ServerId("localhost", server2),
                                                              new AsyncTasksConfiguration(TimeSpan.FromDays(1)),
                                                              new AsyncTasksConfiguration(TimeSpan.FromDays(1)));
                net2.SetDistributor(distributor2);
                var receive2 = new ProxyNetReceiver(distributor2, netReceive2);

                GlobalQueue.SetQueue(q3);

                var netReceive3 = new NetReceiverConfiguration(server3, "localhost", "testService");
                var net3 = new ProxyNetModule(netconfig,
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
                var distributor3 = new ProxyDistributorModule(new AsyncProxyCache(TimeSpan.FromMinutes(100)), net3, queue, new ServerId("localhost", server3),
                                                              new AsyncTasksConfiguration(TimeSpan.FromDays(1)),
                                                              new AsyncTasksConfiguration(TimeSpan.FromDays(1)));
                net3.SetDistributor(distributor3);
                var receive3 = new ProxyNetReceiver(distributor3, netReceive3);

                var distrconfig = new DistributorHashConfiguration(2);
                var queueconfig = new QueueConfiguration(1, 100);
                var dnet = new DistributorNetModule(netconfig,
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
                var ddistributor = new DistributorModule(new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                    new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)), distrconfig,
                    queueconfig, dnet, new ServerId("localhost", server4),
                    new ServerId("localhost", server42),
                    new HashMapConfiguration(filename1, HashMapCreationMode.CreateNew, 1, 1, HashFileType.Distributor));
                dnet.SetDistributor(ddistributor);

                var cache = new DistributorTimeoutCache(
                    new DistributorCacheConfiguration(new TimeSpan(), new TimeSpan()));
                var tranc = new TransactionModule(dnet, new TransactionConfiguration(4), 1, cache);
                var main = new MainLogicModule(ddistributor, tranc, cache);
                var netReceive4 = new NetReceiverConfiguration(server4, "localhost", "testService");
                var netReceive42 = new NetReceiverConfiguration(server42, "localhost", "testService");

                var input = new InputModuleWithParallel(new QueueConfiguration(1, 1), main, tranc);
                var receiver4 = new NetDistributorReceiver(main, input, ddistributor, netReceive4, netReceive42);

                GlobalQueue.SetQueue(q1);
                var dnet2 = new DistributorNetModule(netconfig,
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
                var ddistributor2 = new DistributorModule(new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                    new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)), distrconfig,
                    queueconfig, dnet2,
                    new ServerId("localhost", server5),
                    new ServerId("localhost", server52),
                    new HashMapConfiguration(filename2, HashMapCreationMode.CreateNew, 1, 1, HashFileType.Distributor));
                dnet2.SetDistributor(ddistributor2);

                var netReceive5 = new NetReceiverConfiguration(server5, "localhost", "testService");
                var netReceive52 = new NetReceiverConfiguration(server52, "localhost", "testService");
                var receiver5 = new NetDistributorReceiver(main,
                                                           new InputModuleWithParallel(new QueueConfiguration(1, 1), main,
                                                                                       tranc),
                                                           ddistributor2, netReceive5, netReceive52);

                receive.Start();
                receive2.Start();
                receive3.Start();
                receiver4.Start();
                receiver5.Start();
                net.Start();
                net2.Start();
                net3.Start();
                distributor.Start();
                distributor2.Start();
                distributor3.Start();

                dnet.Start();
                dnet2.Start();
                ddistributor.Start();
                ddistributor2.Start();

                q1.Start();
                q2.Start();
                q3.Start();

                distributor.SayIAmHere(new ServerId("localhost", server42));
                distributor2.SayIAmHere(new ServerId("localhost", server42));

                var dsm1 = (DistributorSystemModel)distributor.GetField("_distributorSystemModel");
                var dsm2 = (DistributorSystemModel)distributor2.GetField("_distributorSystemModel");

                Assert.Equal(1, dsm1.GetDistributorsList().Count);
                Assert.Equal(1, dsm2.GetDistributorsList().Count);

                ddistributor2.SayIAmHereRemoteResult(new ServerId("localhost", server42));

                Thread.Sleep(TimeSpan.FromMilliseconds(300));

                var mad1 =
                    (Impl.DistributorModules.Model.DistributorSystemModel)
                        ddistributor.GetField("_modelOfAnotherDistributors");

                var mad2 =
                    (Impl.DistributorModules.Model.DistributorSystemModel)
                        ddistributor2.GetField("_modelOfAnotherDistributors");

                Thread.Sleep(400);

                Assert.Equal(1, mad1.GetDistributorList().Count);
                Assert.Equal(1, mad2.GetDistributorList().Count);

                distributor3.SayIAmHere(new ServerId("localhost", server52));

                var dsm3 = (DistributorSystemModel)distributor3.GetField("_distributorSystemModel");

                Assert.Equal(2, dsm3.GetDistributorsList().Count);

                q1.Dispose();
                q2.Dispose();
                q3.Dispose();

                net.Dispose();
                net2.Dispose();
                net3.Dispose();
                dnet.Dispose();
                dnet2.Dispose();

                ddistributor.Dispose();
                ddistributor2.Dispose();

                distributor.Dispose();
                distributor2.Dispose();
                distributor3.Dispose();
            }
        }
    }
}

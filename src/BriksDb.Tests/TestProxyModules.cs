using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.NetResults;
using Qoollo.Impl.Common.NetResults.Event;
using Qoollo.Impl.Common.NetResults.System.Distributor;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DistributorModules;
using Qoollo.Impl.DistributorModules.Caches;
using Qoollo.Impl.DistributorModules.DistributorNet;
using Qoollo.Impl.DistributorModules.Interfaces;
using Qoollo.Impl.DistributorModules.ParallelWork;
using Qoollo.Impl.DistributorModules.Transaction;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.Proxy;
using Qoollo.Impl.Proxy.Caches;
using Qoollo.Impl.Proxy.Interfaces;
using Qoollo.Impl.Proxy.Model;
using Qoollo.Impl.Proxy.ProxyNet;
using Qoollo.Tests.NetMock;
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
            var proxy = TestProxySystem(proxyServer);
            proxy.Build(new TestInjectionModule());
            proxy.Start();

            var distrServer = ServerId(distrServer1);
            var distributor = TestHelper.OpenDistributorHost(_kernel, ServerId(distrServer1));
            proxy.Distributor.SayIAmHere(distrServer);

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
            distributor.Dispose();
        }

        [Fact]
        public void ProxySystem_Read_ReadFromFakeDistributor_ExpectedData()
        {
            var proxy = TestProxySystem(proxyServer);
            proxy.Build(new TestInjectionModule());
            proxy.Start();

            var distrServer = ServerId(distrServer1);
            var distributor = TestHelper.OpenDistributorHost(_kernel, distrServer);
            proxy.Distributor.SayIAmHere(distrServer);

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
            distributor.Dispose();
        }

        [Fact]
        public void AsyncProxyCache_AddToCache_WaitRemovedCallback_ExpiredData()
        {
            var cache = new AsyncProxyCache(TimeSpan.FromMilliseconds(200));
            var ev = new InnerData(new Transaction("123", ""))
            {
                Transaction = { UserSupportCallback = new TaskCompletionSource<UserTransaction>() }
            };
            var wait = ev.Transaction.UserSupportCallback.Task;

            cache.AddToCache("123", ev.Transaction);

            wait.Wait();
            Assert.True(wait.Result.IsError);

            cache.Dispose();
        }

        [Fact]
        public void ProxyNetModule_Process_SendDataToDistributors_2SuccessAnd1Fail()
        {
            var server1 = ServerId(distrServer1);
            var server2 = ServerId(distrServer12);
            var server3 = ServerId(distrServer2);

            var s1 = TestHelper.OpenDistributorHost(_kernel, server1);
            var s2 = TestHelper.OpenDistributorHost(_kernel, server2);

            var queue = GetBindedQueue();

            var net = ProxyNetModule();
            AsyncProxyCache();

            var distr = new TestProxyDistributorModule(_kernel);
            _kernel.Bind<IProxyDistributorModule>().ToConstant(distr);

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
            var server1 = ServerId(storageServer1);
            var server2 = ServerId(storageServer2);

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
            var server1 = ServerId(distrServer1);
            var server2 = ServerId(distrServer2);
            var server3 = ServerId(distrServer12);

            var s1 = TestHelper.OpenDistributorHost(_kernel, server1);
            var s2 = TestHelper.OpenDistributorHost(_kernel, server2);

            var queue = GetBindedQueue();

            var net = ProxyNetModule();            
            var distributor = ProxyDistributorModule(net, server1.Port);
            _kernel.Bind<IProxyDistributorModule>().ToConstant(distributor);

            var cache = new ProxyCache(TimeSpan.FromSeconds(20));
            _kernel.Bind<IProxyCache>().ToConstant(cache);

            var main = new ProxyMainLogicModule(_kernel);

            net.Start();

            distributor.Start();

            distributor.SayIAmHere(server1);
            distributor.SayIAmHere(server2);
            distributor.SayIAmHere(server3);

            cache.Start();
            main.Start();

            const string hash = "";
            var ev = new InnerData(new Transaction("", ""))
            {
                Transaction = distributor.CreateTransaction(hash),
                DistributorData = new DistributorData {Destination = new List<ServerId> {server1}}
            };

            var res = main.Process(ev);

            var server = cache.Get(ev.Transaction.DataHash);
            Assert.Null(server);
            Assert.True(res);

            main.Dispose();
            distributor.Dispose();
            net.Dispose();
            cache.Dispose();

            s1.Dispose();
            s2.Dispose();
        }

        [Fact]
        public void ProxyDistributorModule_SayIAmHere_AddDistributor()
        {
            const int replicsCount = 2;

            var filename1 = nameof(ProxyDistributorModule_SayIAmHere_AddDistributor)+"1";
            var filename2 = nameof(ProxyDistributorModule_SayIAmHere_AddDistributor)+"2";
            using (new FileCleaner(filename1))
            using (new FileCleaner(filename2))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                var q1 = GetBindedQueue();
                var net = ProxyNetModule();
                var distributor = ProxyDistributorModule(net, storageServer1);
                _kernel.Rebind<IProxyDistributorModule>().ToConstant(distributor);

                var receive = new ProxyNetReceiver(_kernel, NetReceiverConfiguration(storageServer1));
                receive.Start();

                var q2 = GetBindedQueue();
                var net2 = ProxyNetModule();
                var distributor2 = ProxyDistributorModule(net2, storageServer2);
                _kernel.Rebind<IProxyDistributorModule>().ToConstant(distributor2);

                var receive2 = new ProxyNetReceiver(_kernel, NetReceiverConfiguration(storageServer2));
                receive2.Start();

                var q3 = GetBindedQueue();
                var net3 = ProxyNetModule();
                var distributor3 = ProxyDistributorModule(net3, storageServer3);
                _kernel.Rebind<IProxyDistributorModule>().ToConstant(distributor3);

                var receive3 = new ProxyNetReceiver(_kernel, NetReceiverConfiguration(storageServer3));
                receive3.Start();

                var dnet = DistributorNetModule();
                var ddistributor = DistributorDistributorModule(filename1, replicsCount, dnet, 30000, 30000);
                _kernel.Rebind<IDistributorModule>().ToConstant(ddistributor);
                dnet.Start();

                ddistributor.Start();

                var cache = new DistributorTimeoutCache(DistributorCacheConfiguration(200000, 200000));
                _kernel.Bind<IDistributorTimeoutCache>().ToConstant(cache);

                var tranc = new TransactionModule(_kernel, new TransactionConfiguration(4), 1);
                _kernel.Bind<ITransactionModule>().ToConstant(tranc);

                var main = new MainLogicModule(_kernel);
                _kernel.Bind<IMainLogicModule>().ToConstant(main);
                main.Start();

                var input = new InputModuleWithParallel(_kernel, QueueConfiguration);
                _kernel.Bind<IInputModule>().ToConstant(input);

                var receiver4 = new NetDistributorReceiver(_kernel, 
                    NetReceiverConfiguration(distrServer1),
                    NetReceiverConfiguration(distrServer12));
                receiver4.Start();

                _kernel.Rebind<IGlobalQueue>().ToConstant(q1);

                var dnet2 = DistributorNetModule();
                var ddistributor2 = DistributorDistributorModule(filename2, replicsCount, dnet2, 200000, 30000,
                    distrServer2, distrServer22);
                _kernel.Rebind<IDistributorModule>().ToConstant(ddistributor2);
                dnet2.Start();

                var receiver5 = new NetDistributorReceiver(_kernel, 
                    NetReceiverConfiguration(distrServer2),
                    NetReceiverConfiguration(distrServer22));
                
                receiver5.Start();
                distributor.Start();
                distributor2.Start();
                distributor3.Start();

                ddistributor2.Start();

                q1.Start();
                q2.Start();
                q3.Start();

                distributor.SayIAmHere(new ServerId("localhost", distrServer12));
                distributor2.SayIAmHere(new ServerId("localhost", distrServer12));

                var dsm1 = (DistributorSystemModel)distributor.GetField("_distributorSystemModel");
                var dsm2 = (DistributorSystemModel)distributor2.GetField("_distributorSystemModel");

                Assert.Equal(1, dsm1.GetDistributorsList().Count);
                Assert.Equal(1, dsm2.GetDistributorsList().Count);

                ddistributor2.SayIAmHereRemoteResult(new ServerId("localhost", distrServer12));

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

                distributor3.SayIAmHere(new ServerId("localhost", distrServer22));

                var dsm3 = (DistributorSystemModel)distributor3.GetField("_distributorSystemModel");

                Assert.Equal(2, dsm3.GetDistributorsList().Count);

                q1.Dispose();
                q2.Dispose();
                q3.Dispose();

                ddistributor.Dispose();
                ddistributor2.Dispose();

                distributor.Dispose();
                distributor2.Dispose();
                distributor3.Dispose();
            }
        }
    }
}

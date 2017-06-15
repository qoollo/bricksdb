using System;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
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
using Xunit;
using Consts = Qoollo.Impl.Common.Support.Consts;
using SingleConnectionToDistributor = Qoollo.Impl.Writer.WriterNet.SingleConnectionToDistributor;

namespace Qoollo.Tests
{
    [Collection("test collection 1")]
    public class TestDistributorModules : TestBase
    {
        [Fact]
        public void WriterSystemModel_GetUnavailableServers_CheckAvailableAndUnAvailableServers()
        {
            var filename = nameof(WriterSystemModel_GetUnavailableServers_CheckAvailableAndUnAvailableServers);
            using (new FileCleaner(filename))
            {
                var server1 = ServerId(storageServer1);
                var server2 = ServerId(storageServer2);
                var server3 = ServerId(storageServer3);

                var config = new DistributorHashConfiguration(1);

                var writer =
                    new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 6, 3,
                        HashFileType.Collector));
                writer.CreateMap();
                writer.SetServer(0, server1.RemoteHost, server1.Port, 157);
                writer.SetServer(1, server2.RemoteHost, server2.Port, 157);
                writer.SetServer(2, server3.RemoteHost, server3.Port, 157);
                writer.SetServer(3, server1.RemoteHost, server1.Port, 157);
                writer.SetServer(4, server2.RemoteHost, server2.Port, 157);
                writer.SetServer(5, server3.RemoteHost, server3.Port, 157);
                writer.Save();

                var model = new WriterSystemModel(config,
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Writer));
                model.Start();

                model.ServerNotAvailable(server1);
                Assert.Equal(1, model.GetUnavailableServers().Count);
                model.ServerNotAvailable(server1);
                Assert.Equal(1, model.GetUnavailableServers().Count);
                model.ServerNotAvailable(server2);
                Assert.Equal(2, model.GetUnavailableServers().Count);
                model.ServerNotAvailable(server3);
                Assert.Equal(3, model.GetUnavailableServers().Count);
                model.ServerAvailable(server1);
                Assert.Equal(2, model.GetUnavailableServers().Count);
                model.ServerAvailable(server1);
                Assert.Equal(2, model.GetUnavailableServers().Count);
                model.ServerAvailable(server2);
                Assert.Equal(1, model.GetUnavailableServers().Count);
                model.ServerAvailable(server3);
                Assert.Equal(0, model.GetUnavailableServers().Count);
            }
        }

        [Theory]
        [InlineData(1)]
        public void MainLogicModule_TransactionAnswerResult_ReceiveAnswersFromWriter(int countReplics)
        {
            var filename = nameof(MainLogicModule_TransactionAnswerResult_ReceiveAnswersFromWriter);
            using (new FileCleaner(filename))
            {
                #region hell

                var distrconfig = new DistributorHashConfiguration(countReplics);
                var dnet = DistributorNetModule();
                var ddistributor = DistributorDistributorModule(filename, countReplics, dnet, 3000, 3000);
                dnet.SetDistributor(ddistributor);

                var cache = new DistributorTimeoutCache(DistributorCacheConfiguration());
                var tranc = new TransactionModule(dnet, new TransactionConfiguration(4), distrconfig.CountReplics, cache);
                var main = new MainLogicModule(ddistributor, tranc, cache);

                var input = new InputModuleWithParallel(new QueueConfiguration(2, 100000), main, tranc);
                var netDistributorReceiver = new NetDistributorReceiver(main, input, ddistributor,
                    NetReceiverConfiguration(distrServer1),
                    NetReceiverConfiguration(distrServer12));

                ddistributor.Start();
                netDistributorReceiver.Start();

                #endregion

                var t = 0;
                GlobalQueue.Queue.TransactionQueue.Registrate(data => Interlocked.Increment(ref t));
                GlobalQueue.Queue.Start();

                var connection = new SingleConnectionToDistributor(ServerId(distrServer1), ConnectionConfiguration,
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
                connection.Connect();

                connection.TransactionAnswerResult(new Transaction("123", "123"));
                Thread.Sleep(TimeSpan.FromMilliseconds(100));
                connection.TransactionAnswerResult(new Transaction("1243", "1423"));
                Thread.Sleep(TimeSpan.FromMilliseconds(100));
                Assert.Equal(2, t);

                connection.Dispose();
                netDistributorReceiver.Dispose();
                GlobalQueue.Queue.Dispose();
                cache.Dispose();
                dnet.Dispose();
                ddistributor.Dispose();
                tranc.Dispose();
                input.Dispose();
            }
        }

        [Fact]
        public void TransactionModule_ProcessSyncWithExecutor_NoServersToSendData()
        {
            var s1 = new TestServerDescription(1);
            var s2 = new TestServerDescription(2);

            var cache = new DistributorTimeoutCache(DistributorCacheConfiguration());

            var net = new NetModuleTest(new Dictionary<ServerId, bool> {{s1, false}, {s2, true}});
            var trm = new TransactionModule(net, new TransactionConfiguration(1), 2, cache);

            trm.Start();

            GlobalQueue.Queue.Start();

            var data = new InnerData(new Transaction("123", ""))
            {
                Transaction =
                {
                    OperationName = OperationName.Create
                },
                DistributorData = new DistributorData {Destination = new List<ServerId> {s1, s2}}
            };
            cache.AddDataToCache(data);

            using (var trans = trm.Rent())
            {
                trm.ProcessWithExecutor(data, trans.Element);
            }

            Thread.Sleep(1000);

            Assert.True(data.Transaction.IsError);

            trm.Dispose();
            GlobalQueue.Queue.Dispose();
            cache.Dispose();
        }

        [Theory]
        [InlineData(2)]
        public void TransactionModule_ProcessSyncWithExecutor_SuccessSendDataToServers(int countReplics)
        {
            var filename = nameof(TransactionModule_ProcessSyncWithExecutor_SuccessSendDataToServers);
            using (new FileCleaner(filename))
            {
                var server1 = ServerId(storageServer1);
                var server2 = ServerId(storageServer2);

                var distributor = DistributorDistributorModule(filename, countReplics, null, 3000, 3000);
                var net = DistributorNetModule();

                net.SetDistributor(distributor);
                distributor.Start();
                net.Start();

                var s1 = TestHelper.OpenWriterHost(server1.Port);
                var s2 = TestHelper.OpenWriterHost(server2.Port);

                net.ConnectToWriter(server1);
                net.ConnectToWriter(server2);

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                var ev = new InnerData(new Transaction("", ""))
                {
                    DistributorData = new DistributorData {Destination = new List<ServerId> {server1, server2}},
                };

                var trm = new TransactionModule(net, new TransactionConfiguration(1), countReplics,
                    new DistributorTimeoutCache(DistributorCacheConfiguration()));
                trm.Start();

                using (var trans = trm.Rent())
                {
                    trm.ProcessWithExecutor(ev, trans.Element);
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(100));

                Assert.Equal(1, s1.Value);
                Assert.Equal(1, s2.Value);
                Assert.False(ev.Transaction.IsError);

                net.Dispose();
                distributor.Dispose();
                trm.Dispose();
                s1.Dispose();
                s2.Dispose();
            }
        }

        [Fact]
        public void TransactionModule_ProcessSyncWithExecutor_RollbackNoEnoughServers()
        {
            var filename = nameof(TransactionModule_ProcessSyncWithExecutor_SuccessSendDataToServers);
            using (new FileCleaner(filename))
            {
                var server1 = ServerId(storageServer1);
                var server2 = ServerId(storageServer2);
                var server3 = ServerId(storageServer3);

                #region hell

                var distributor = DistributorDistributorModule(filename, 2, null, 3000, 3000);
                var net = DistributorNetModule();

                net.SetDistributor(distributor);
                distributor.Start();
                net.Start();

                var cache = new DistributorTimeoutCache(DistributorCacheConfiguration());
                var trm = new TransactionModule(net, new TransactionConfiguration(1), 3, cache);
                trm.Start();

                GlobalQueue.Queue.Start();

                var s1 = TestHelper.OpenWriterHost(server1.Port);
                var s2 = TestHelper.OpenWriterHost(server2.Port);

                net.ConnectToWriter(server1);
                net.ConnectToWriter(server2);

                #endregion

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                var data = new InnerData(new Transaction("123", ""))
                {
                    Transaction = {OperationName = OperationName.Create},
                    DistributorData =
                        new DistributorData {Destination = new List<ServerId> {server1, server2, server3},}
                };
                cache.AddDataToCache(data);

                using (var trans = trm.Rent())
                {
                    trm.ProcessWithExecutor(data, trans.Element);
                }

                Thread.Sleep(200);
                GlobalQueue.Queue.TransactionQueue.Add(data.Transaction);
                GlobalQueue.Queue.TransactionQueue.Add(data.Transaction);
                Thread.Sleep(2000);

                Assert.True(s1.Value <= 0);
                Assert.True(s2.Value <= 0);
                Assert.True(data.Transaction.IsError);

                net.Dispose();
                trm.Dispose();
                s1.Dispose();
                s2.Dispose();

                cache.Dispose();
                GlobalQueue.Queue.Dispose();
                distributor.Dispose();
            }
        }

        [Fact]
        public void NetModule_Process_SendDatatoAvaliableAndUnavalilableServers()
        {
            var filename = nameof(NetModule_Process_SendDatatoAvaliableAndUnavalilableServers);
            using (new FileCleaner(filename))
            {
                var server1 = ServerId(storageServer1);
                var server2 = ServerId(storageServer2);
                var server3 = ServerId(storageServer3);

                var s1 = TestHelper.OpenWriterHost(server1.Port);
                var s2 = TestHelper.OpenWriterHost(server2.Port);

                var net = DistributorNetModule();
                var distributor = DistributorDistributorModule(filename, 2, net, 3000, 3000);

                net.SetDistributor(distributor);
                distributor.Start();
                net.Start();
                GlobalQueue.Queue.Start();

                net.ConnectToWriter(server1);
                net.ConnectToWriter(server2);

                var ev = new InnerData(new Transaction("default", "default"))
                {
                    DistributorData = new DistributorData {Destination = new List<ServerId> {server1}},
                };

                var ret1 = net.Process(server1, ev);
                var ret2 = net.Process(server2, ev);
                var ret3 = net.Process(server3, ev);
                Assert.Equal(1, s1.Value);
                Assert.Equal(1, s2.Value);
                Assert.Equal(typeof (SuccessResult), ret1.GetType());
                Assert.Equal(typeof (SuccessResult), ret2.GetType());
                Assert.Equal(typeof (ServerNotFoundResult), ret3.GetType());

                GlobalQueue.Queue.Dispose();
                net.Dispose();
                distributor.Dispose();

                s1.Dispose();
                s2.Dispose();
            }
        }

        [Fact]
        public void DistributorTimeoutCache_GetUpdate()
        {
            var cache = new DistributorTimeoutCache(DistributorCacheConfiguration(200, 500));
            var ev = new InnerData(new Transaction("123", ""))
            {
                DistributorData = new DistributorData {Destination = new List<ServerId>()},
            };

            cache.AddToCache("123", ev);
            var ret = cache.Get("123");
            Assert.Equal(ev, ret);
            ev.Transaction.Complete();
            cache.Update("123", ev);
            ret = cache.Get("123");
            Assert.Equal(ev, ret);
            Thread.Sleep(200);
            ret = cache.Get("123");
            Assert.Equal(ev, ret);
            Assert.Equal(TransactionState.Complete, ev.Transaction.State);
            Thread.Sleep(500);
            ret = cache.Get("123");
            Assert.Equal(null, ret);

            cache.Dispose();
        }

        [Fact]
        public void DistributorTimeoutCache_TimeoutData_SendToMainLogicModuleObsoleteData()
        {
            var cache = new DistributorTimeoutCache(DistributorCacheConfiguration(200, 500));
            var net = DistributorNetModule();
            var trans = new TransactionModule(net, new TransactionConfiguration(1), 1, cache);

            var ev = new InnerData(new Transaction("123", "") {OperationName = OperationName.Create})
            {
                DistributorData = new DistributorData {Destination = new List<ServerId>()},
            };

            ev.Transaction.Complete();
            cache.AddToCache(ev.Transaction.CacheKey, ev);
            var ret = cache.Get(ev.Transaction.CacheKey);
            Assert.Equal(ev, ret);
            Thread.Sleep(200);
            cache.Get(ev.Transaction.CacheKey);
            Assert.Equal(TransactionState.Complete, ev.Transaction.State);
            Thread.Sleep(200);
            cache.Get(ev.Transaction.CacheKey);
            Assert.Equal(TransactionState.Complete, ev.Transaction.State);
            Thread.Sleep(500);
            ret = cache.Get(ev.Transaction.CacheKey);
            Assert.Equal(null, ret);

            ev = new InnerData(new Transaction("1231", "") {OperationName = OperationName.Create})
            {
                DistributorData = new DistributorData {Destination = new List<ServerId>()},
            };

            ev.Transaction.StartTransaction();
            cache.AddToCache(ev.Transaction.CacheKey, ev);
            ret = cache.Get(ev.Transaction.CacheKey);
            Assert.Equal(ev, ret);
            Thread.Sleep(200);
            cache.Get(ev.Transaction.CacheKey);
            Assert.Equal(TransactionState.Error, ev.Transaction.State);
            Thread.Sleep(200);
            ev = cache.Get(ev.Transaction.CacheKey);
            Assert.Equal(TransactionState.Error, ev.Transaction.State);
            Thread.Sleep(500);
            ret = cache.Get(ev.Transaction.CacheKey);
            Assert.Equal(null, ret);

            cache.Dispose();
        }

        [Fact]
        public void WriterSystemModel_GetDestination_ChechAvailableServers()
        {
            var filename = nameof(WriterSystemModel_GetDestination_ChechAvailableServers);
            using (new FileCleaner(filename))
            {
                var writer =
                    new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 6, 3,
                        HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "local", 11010, 157);
                writer.SetServer(1, "local", 11011, 157);
                writer.SetServer(2, "local", 11012, 157);
                writer.SetServer(3, "local", 11010, 157);
                writer.SetServer(4, "local", 11011, 157);
                writer.SetServer(5, "local", 11012, 157);
                writer.Save();

                var model = WriterSystemModel(filename, 1);
                model.Start();

                var ev = new InnerData(new Transaction("123", ""))
                {
                    Transaction = new Transaction(HashConvertor.GetString("1"), ""),
                    DistributorData = new DistributorData {Destination = new List<ServerId>()},
                };

                var ret = model.GetDestination(ev);
                Assert.True(ret.Count == 1);
                model.ServerNotAvailable(ret.First());
                var ret2 = model.GetDestination(ev);
                Assert.True(ret2.Count == 1);
                Assert.NotEqual(ret.First(), ret2.First());
                model.ServerNotAvailable(ret2.First());
                var ret3 = model.GetDestination(ev);
                Assert.True(ret3.Count == 1);
                Assert.NotEqual(ret.First(), ret3.First());
                Assert.NotEqual(ret3.First(), ret2.First());
                model.ServerNotAvailable(ret3.First());
                var ret4 = model.GetDestination(ev);
                Assert.True(ret4.Count == 0);
            }
        }

        [Fact]
        public void MainLogic_ProcessWithData_SendAllReplicsThenObsoleteDataInCache()
        {
            var filename = nameof(MainLogic_ProcessWithData_SendAllReplicsThenObsoleteDataInCache);
            using (new FileCleaner(filename))
            {
                CreateHashFile(filename, 2);

                var distrconfig = new DistributorHashConfiguration(2);

                var cache = new DistributorTimeoutCache(DistributorCacheConfiguration(400, 1000));
                var net = DistributorNetModule();
                var distributor = DistributorDistributorModule(filename, 2, net, 3000, 3000);

                net.SetDistributor(distributor);

                var transaction = new TransactionModule(net, new TransactionConfiguration(1),
                    distrconfig.CountReplics, cache);
                var main = new MainLogicModule(distributor, transaction, cache);

                var server1 = ServerId(storageServer1);
                var server2 = ServerId(storageServer2);

                var s1 = TestHelper.OpenWriterHost(server1.Port);
                var s2 = TestHelper.OpenWriterHost(server2.Port);

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
                    Transaction = new Transaction(HashConvertor.GetString("1"), "default")
                    {
                        OperationName = OperationName.Create,
                        OperationType = OperationType.Async
                    },
                    DistributorData = new DistributorData {Destination = new List<ServerId>()},
                };

                using (var trans = transaction.Rent())
                {
                    main.ProcessWithData(ev, trans.Element);
                }

                GlobalQueue.Queue.TransactionQueue.Add(ev.Transaction);
                GlobalQueue.Queue.TransactionQueue.Add(ev.Transaction);
                Thread.Sleep(TimeSpan.FromMilliseconds(300));

                Assert.True(s1.Value > 0);
                Assert.True(s2.Value > 0);
                Assert.Equal(main.GetTransactionState(ev.Transaction.UserTransaction).State, TransactionState.Complete);
                Thread.Sleep(TimeSpan.FromMilliseconds(1000));
                Assert.Equal(main.GetTransactionState(ev.Transaction.UserTransaction).State, TransactionState.DontExist);

                net.Dispose();
                distributor.Dispose();
                transaction.Dispose();
                main.Dispose();
                cache.Dispose();
                GlobalQueue.Queue.Dispose();

                s1.Dispose();
                s2.Dispose();
            }
        }

        [Fact]
        public void WriterSystemModel_GetDestination_CountReplics()
        {
            var filename = nameof(WriterSystemModel_GetDestination_CountReplics);
            using (new FileCleaner(filename))
            {
                var writer =
                    new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 6, 3,
                        HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "local", 11010, 157);
                writer.SetServer(1, "local", 11011, 157);
                writer.SetServer(2, "local", 11012, 157);
                writer.SetServer(3, "local", 11010, 157);
                writer.SetServer(4, "local", 11011, 157);
                writer.SetServer(5, "local", 11012, 157);
                writer.Save();

                var model = WriterSystemModel(filename, 4);
                model.Start();

                var ev = new InnerData(new Transaction("123", ""))
                {
                    Transaction = new Transaction(HashConvertor.GetString("1"), ""),
                    DistributorData = new DistributorData {Destination = new List<ServerId>()},
                };

                var ret = model.GetDestination(ev);
                Assert.True(ret.Count == 0);
                model = new WriterSystemModel(new DistributorHashConfiguration(3),
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Distributor));
                model.Start();

                ret = model.GetDestination(ev);
                Assert.Equal(3, ret.Count);
                Assert.NotEqual(ret[0], ret[1]);
                Assert.NotEqual(ret[0], ret[2]);
                Assert.NotEqual(ret[2], ret[1]);
            }
        }

        [Fact]
        public void InputModuleWithParallel_ProcessAsync_SendToOneServers_Success()
        {
            var filename = nameof(InputModuleWithParallel_ProcessAsync_SendToOneServers_Success);
            using (new FileCleaner(filename))
            {
                var q1 = new GlobalQueueInner();

                CreateHashFile(filename, 1);

                #region hell

                GlobalQueue.SetQueue(q1);

                var dnet = DistributorNetModule();
                var ddistributor = DistributorDistributorModule(filename, 1, dnet, 3000, 3000);
                dnet.SetDistributor(ddistributor);

                var cache = new DistributorTimeoutCache(DistributorCacheConfiguration(20000, 20000));
                var tranc = new TransactionModule(dnet, new TransactionConfiguration(4), 1, cache);

                var main = new MainLogicModule(ddistributor, tranc, cache);

                var input = new InputModuleWithParallel(new QueueConfiguration(2, 100000), main, tranc);
                var netDistributorReceiver = new NetDistributorReceiver(main, input, ddistributor,
                    NetReceiverConfiguration(distrServer1),
                    NetReceiverConfiguration(distrServer12));

                #endregion

                var s = TestHelper.OpenWriterHost(storageServer1);

                tranc.Start();
                main.Start();
                netDistributorReceiver.Start();
                input.Start();
                dnet.Start();
                ddistributor.Start();

                q1.Start();

                var list = new List<InnerData>();
                const int count = 100;
                for (int i = 1; i < count + 1; i++)
                {
                    var ev =
                        new InnerData(new Transaction(
                            HashConvertor.GetString(i.ToString(CultureInfo.InvariantCulture)), "")
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

                Assert.Equal(count, s.Value);

                foreach (var data in list)
                {
                    var transaction = main.GetTransactionState(data.Transaction.UserTransaction);
                    Assert.Equal(TransactionState.TransactionInProcess, transaction.State);
                }
                foreach (var data in list)
                {
                    q1.TransactionQueue.Add(data.Transaction);
                }
                Thread.Sleep(TimeSpan.FromMilliseconds(1000));
                foreach (var data in list)
                {
                    var transaction = main.GetTransactionState(data.Transaction.UserTransaction);
                    Assert.Equal(TransactionState.Complete, transaction.State);
                }
                q1.Dispose();

                input.Dispose();
                ddistributor.Dispose();
                dnet.Dispose();
                cache.Dispose();
                tranc.Dispose();
                netDistributorReceiver.Dispose();

                s.Dispose();
            }
        }

        [Fact]
        public void InputModuleWithParallel_ProcessAsync_SendToTwoServers_Success()
        {
            var filename = nameof(InputModuleWithParallel_ProcessAsync_SendToTwoServers_Success);
            using (new FileCleaner(filename))
            {
                var q1 = new GlobalQueueInner();

                CreateHashFile(filename, 2);

                #region hell

                GlobalQueue.SetQueue(q1);

                var dnet = DistributorNetModule();
                var ddistributor = DistributorDistributorModule(filename, 2, dnet, 3000, 3000);
                dnet.SetDistributor(ddistributor);

                var cache = new DistributorTimeoutCache(DistributorCacheConfiguration(2000000, 200000));
                var tranc = new TransactionModule(dnet, new TransactionConfiguration(4), 2, cache);
                var main = new MainLogicModule(ddistributor, tranc, cache);

                var input = new InputModuleWithParallel(new QueueConfiguration(2, 100000), main, tranc);
                var netDistributorReceiver = new NetDistributorReceiver(main, input, ddistributor,
                    NetReceiverConfiguration(distrServer1),
                    NetReceiverConfiguration(distrServer12));

                #endregion

                var s1 = TestHelper.OpenWriterHost(storageServer1);
                var s2 = TestHelper.OpenWriterHost(storageServer2);

                tranc.Start();
                main.Start();
                netDistributorReceiver.Start();
                input.Start();
                dnet.Start();
                ddistributor.Start();

                q1.Start();

                var list = new List<InnerData>();
                const int count = 100;
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
                            Data = CommonDataSerializer.Serialize(i)
                        };
                    list.Add(ev);
                }

                foreach (var data in list)
                {
                    input.ProcessAsync(data);
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(1200));

                Assert.Equal(count, s1.Value);
                Assert.Equal(count, s2.Value);

                foreach (var data in list)
                {
                    var transaction = main.GetTransactionState(data.Transaction.UserTransaction);
                    Assert.Equal(TransactionState.TransactionInProcess, transaction.State);
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
                    Assert.Equal(TransactionState.Complete, transaction.State);
                }
                q1.Dispose();

                ddistributor.Dispose();
                dnet.Dispose();
                cache.Dispose();
                input.Dispose();
                tranc.Dispose();
                netDistributorReceiver.Dispose();

                s1.Dispose();
                s2.Dispose();
            }
        }

        [Fact]
        public void InputModuleWithParallel_ProcessAsync_SendToOneServersAndTimeoutInCache_Success()
        {
            var filename = nameof(InputModuleWithParallel_ProcessAsync_SendToOneServersAndTimeoutInCache_Success);
            using (new FileCleaner(filename))
            {
                var q1 = new GlobalQueueInner();

                CreateHashFile(filename, 1);

                #region hell

                GlobalQueue.SetQueue(q1);

                var dnet = DistributorNetModule();
                var ddistributor = DistributorDistributorModule(filename, 1, dnet, 3000, 3000);
                dnet.SetDistributor(ddistributor);

                var cache = new DistributorTimeoutCache(DistributorCacheConfiguration());
                var tranc = new TransactionModule(dnet, new TransactionConfiguration(4), 1, cache);
                var main = new MainLogicModule(ddistributor, tranc, cache);

                var input = new InputModuleWithParallel(new QueueConfiguration(2, 100000), main, tranc);
                var netDistributorReceiver = new NetDistributorReceiver(main, input, ddistributor,
                    NetReceiverConfiguration(distrServer1),
                    NetReceiverConfiguration(distrServer12));

                #endregion

                var s = TestHelper.OpenWriterHost(storageServer1);

                main.Start();
                netDistributorReceiver.Start();
                input.Start();
                dnet.Start();
                ddistributor.Start();

                q1.Start();

                var list = new List<InnerData>();
                const int count = 1;
                for (int i = 1; i < count + 1; i++)
                {
                    var ev =
                        new InnerData(new Transaction(
                            HashConvertor.GetString(i.ToString(CultureInfo.InvariantCulture)), "")
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

                Assert.Equal(count, s.Value);

                foreach (var data in list)
                {
                    var transaction = main.GetTransactionState(data.Transaction.UserTransaction);
                    Assert.Equal(TransactionState.TransactionInProcess, transaction.State);
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(2000));
                foreach (var data in list)
                {
                    var transaction = main.GetTransactionState(data.Transaction.UserTransaction);
                    if (transaction.State == TransactionState.DontExist)
                        Thread.Sleep(1000);
                    transaction = main.GetTransactionState(data.Transaction.UserTransaction);
                    Assert.Equal(TransactionState.Error, transaction.State);
                }

                q1.Dispose();

                input.Dispose();
                ddistributor.Dispose();
                dnet.Dispose();
                cache.Dispose();
                tranc.Dispose();
                netDistributorReceiver.Dispose();

                s.Dispose();
            }
        }
    }
}

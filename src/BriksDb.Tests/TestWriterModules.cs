using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Ninject;
using Qoollo.Client.Support;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Components;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.TestSupport;
using Qoollo.Tests.NetMock;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestProxy;
using Qoollo.Tests.TestWriter;
using Xunit;
using Consts = Qoollo.Client.Support.Consts;

namespace Qoollo.Tests
{
    [Collection("test collection 1")]
    public class TestWriterModules:TestBase
    {
        private TestProxySystem _proxy;
        private TestWriterGate _writer1;
        private TestWriterGate _writer2;
        private TestDistributorGate _distributor1;
        private TestDistributorGate _distributor2;

        public TestWriterModules():base()
        {
            InitInjection.Kernel = new StandardKernel(new TestInjectionModule());

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

        [Fact]
        public void DbModule_LocalAndRemoteData_Count()
        {
            var filename = nameof(DbModule_LocalAndRemoteData_Count);
            using (new FileCleaner(filename))
            {
                var provider = new IntHashConvertor();

                var writer =
                    new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 2, 3,
                        HashFileType.Collector));
                writer.CreateMap();
                writer.SetServer(0, "localhost", 157, 157);
                writer.SetServer(1, "localhost", 11011, 157);
                writer.Save();

                _writer1.Build(157, filename, 1);

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
                Assert.NotEqual(count, mem.Local);
                Assert.NotEqual(count, mem.Remote);
                Assert.Equal(count, mem.Local + mem.Remote);

                _writer1.Dispose();
            }
        }

        [Fact]
        public void Writer_ProcessData_SendResultToDistributerMock()
        {
            var filename = nameof(Writer_ProcessData_SendResultToDistributerMock);
            using (new FileCleaner(filename))
            {
                const int distributorServer1 = 22171;
                const int storageServer1 = 22172;

                var writer =
                    new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 1, 1,
                        HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.Save();

                _writer1.Build(storageServer1, filename, 1);
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

                Assert.Equal(count, s.SendValue);
                _writer1.Dispose();
            }
        }

        [Fact]
        public void Writer_ProcessData_SendResultToTwoDistributeMocks()
        {
            var filename = nameof(Writer_ProcessData_SendResultToTwoDistributeMocks);
            using (new FileCleaner(filename))
            {
                const int distributorServer1 = 22173;
                const int distributorServer2 = 22174;
                const int storageServer1 = 22175;

                var writer =
                    new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 1, 1, HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.Save();

                _writer1.Build(storageServer1, filename, 1);
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

                Assert.Equal(count, s.SendValue);
                Assert.Equal(count, s2.SendValue);

                _writer1.Dispose();
            }
        }

        [Fact]
        public void Writer_ProcessDataFromDistributor_SendResultBack()
        {
            var filename = nameof(Writer_ProcessDataFromDistributor_SendResultBack);
            using (new FileCleaner(filename))
            {
                const int distrServer1 = 22180;
                const int distrServer12 = 23180;
                const int storageServer1 = 22181;

                var writer =
                    new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 1, 1,
                        HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.Save();

                _distributor1.Build(1, distrServer1, distrServer12, filename);
                _writer1.Build(storageServer1, filename, 1);

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
                    Assert.Equal(TransactionState.Complete, transaction.State);
                }

                var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
                Assert.Equal(count, mem.Local);

                _writer1.Dispose();
                _distributor1.Dispose();
            }
            
        }

        [Fact]
        public void Writer_ProcessDataFromDistributor_SendResultBack_TwoWriters()
        {
            var filename = nameof(Writer_ProcessDataFromDistributor_SendResultBack_TwoWriters);
            using (new FileCleaner(filename))
            {
                const int distrServer1 = 22182;
                const int distrServer12 = 23182;
                const int storageServer1 = 22183;
                const int storageServer2 = 22184;

                var writer =
                    new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 2, 2,
                        HashFileType.Distributor));

                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                _distributor1.Build(1, distrServer1, distrServer12, filename);

                _writer1.Build(storageServer1, filename, 1);
                _writer2.Build(storageServer2, filename, 1);

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
                    Assert.Equal(TransactionState.Complete, transaction.State);
                }

                var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
                var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;

                Assert.Equal(count, mem.Local + mem2.Local);

                _writer1.Dispose();
                _writer2.Dispose();

                _distributor1.Dispose();
            }
        }

        [Fact]
        public void Writer_ProcessDataFromDistributor_SendResultBack_TwoWritersAndTwoReplics()
        {
            var filename = nameof(Writer_ProcessDataFromDistributor_SendResultBack_TwoWritersAndTwoReplics);
            using (new FileCleaner(filename))
            {
                const int distrServer1 = 22185;
                const int distrServer12 = 23185;
                const int storageServer1 = 22186;
                const int storageServer2 = 22187;

                var writer =
                    new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 2, 2,
                        HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                _distributor1.Build(2, distrServer1, distrServer12, filename);

                _writer1.Build(storageServer1, filename, 1);
                _writer2.Build(storageServer2, filename, 1);

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

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                foreach (var data in list)
                {
                    var transaction = _distributor1.Main.GetTransactionState(data.Transaction.UserTransaction);
                    Assert.Equal(TransactionState.Complete, transaction.State);
                }

                var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
                var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;

                Assert.Equal(count, mem.Local + mem2.Local);
                Assert.Equal(count, mem.Remote + mem2.Remote);

                _writer1.Dispose();
                _writer2.Dispose();

                _distributor1.Dispose();
            }

             
        }

        [Fact]
        public void Writer_ProcessDataFromDistributor_CRUD_TwoWriters()
        {
            var filename = nameof(Writer_ProcessDataFromDistributor_CRUD_TwoWriters);
            using (new FileCleaner(filename))
            {
                const int distrServer1 = 22201;
                const int distrServer12 = 22202;
                const int storageServer1 = 22203;
                const int storageServer2 = 22204;

                var writer =
                    new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 2, 3,
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
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile,
                        1, 1, HashFileType.Distributor), new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                    new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));

                _writer1.Build(storageServer1, filename, 2);
                _writer2.Build(storageServer2, filename, 2);

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
                    Assert.Equal(i + 1, mem.Local + mem2.Local);
                }

                for (int i = 0; i < count; i++)
                {
                    UserTransaction user;
                    var data = api.Read(i, out user);
                    Assert.Equal(i, data);
                }

                for (int i = 0; i < count; i++)
                {
                    var task = api.DeleteSync(i);
                    task.Wait();
                    Assert.Equal(count - i - 1, mem.Local + mem2.Local);
                }

                for (int i = 0; i < count; i++)
                {
                    UserTransaction user;
                    var data = api.Read(i, out user);
                    Assert.Null(data);
                }

                _writer2.Dispose();
                _writer1.Dispose();

                d.Dispose();
                _proxy.Dispose();
            }
            
        }
    }
}

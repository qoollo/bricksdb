using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Qoollo.Client.Support;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Tests.NetMock;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestProxy;
using Qoollo.Tests.TestWriter;
using Xunit;

namespace Qoollo.Tests
{
    [Collection("test collection 1")]
    public class TestWriterModules:TestBase
    {
        private readonly TestProxySystem _proxyTest;
        private readonly TestDistributorGate _distributor1;
        private readonly IntHashConvertor _provider;

        public TestWriterModules():base()
        {
            _proxyTest = TestProxySystem(20, 40);
            _proxyTest.Build(new TestInjectionModule());

            _writer1 = new TestWriterGate();
            _writer2 = new TestWriterGate();
            _distributor1 = new TestDistributorGate();

             _provider = new IntHashConvertor();
        }

        private InnerData InnerData(int i)
        {
            var ev =
                new InnerData(new Transaction(_provider.CalculateHashFromKey(i), "")
                {
                    OperationName = OperationName.Create
                })
                {
                    Data = CommonDataSerializer.Serialize(i),
                    Key = CommonDataSerializer.Serialize(i),
                    Transaction = {Distributor = ServerId(distrServer1)}
                };
            ev.Transaction.TableName = "Int";
            return ev;
        }

        [Theory]
        [InlineData(100)]
        public void DbModule_LocalAndRemoteData_Count(int count)
        {
            var filename = nameof(DbModule_LocalAndRemoteData_Count);
            using (new FileCleaner(filename))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(countReplics: 1, hash: filename);

                _writer1.Build(storageServer1);

                var d = TestHelper.OpenDistributorHostForDb(_kernel, ServerId(distrServer1));
                _writer1.Start();

                for (int i = 0; i < count; i++)
                {
                    var data = InnerData(i);
                    _writer1.Input.Process(data);
                }

                Thread.Sleep(1000);

                var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
                Assert.NotEqual(count, mem.Local);
                Assert.NotEqual(count, mem.Remote);
                Assert.Equal(count, mem.Local + mem.Remote);

                _writer1.Dispose();
                d.Dispose();
            }
        }

        [Theory]
        [InlineData(100)]
        public void Writer_ProcessData_SendResultToDistributerMock(int count)
        {
            var filename = nameof(Writer_ProcessData_SendResultToDistributerMock);
            using (new FileCleaner(filename))
            {
                CreateHashFile(filename, 1);
                CreateConfigFile(countReplics: 1, hash: filename);

                _writer1.Build(storageServer1);
                _writer1.Start();

                var s = TestHelper.OpenDistributorHostForDb(_kernel, ServerId(distrServer1));

                for (int i = 1; i < count + 1; i++)
                {
                    var data = InnerData(i);
                    _writer1.Q.DbInputProcessQueue.Add(data);
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                Assert.Equal(count, s.SendValue);

                _writer1.Dispose();
                s.Dispose();
            }
        }

        [Theory]
        [InlineData(100)]
        public void Writer_ProcessData_SendResultToTwoDistributeMocks(int count)
        {
            var filename = nameof(Writer_ProcessData_SendResultToTwoDistributeMocks);
            using (new FileCleaner(filename))
            {
                CreateHashFile(filename, 1);
                CreateConfigFile(countReplics: 1, hash: filename);

                _writer1.Build(storageServer1);
                _writer1.Start();

                var s = TestHelper.OpenDistributorHostForDb(_kernel, ServerId(distrServer1));
                var s2 = TestHelper.OpenDistributorHostForDb(_kernel, ServerId(distrServer2));

                for (int i = 1; i < count + 1; i++)
                {
                    var data = InnerData(i);
                    data.Transaction.Distributor = ServerId(distrServer1);

                    _writer1.Q.DbInputProcessQueue.Add(data);

                    data = InnerData(i);
                    data.Transaction.Distributor = ServerId(distrServer2);

                    _writer1.Q.DbInputProcessQueue.Add(data);
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                Assert.Equal(count, s.SendValue);
                Assert.Equal(count, s2.SendValue);

                _writer1.Dispose();

                s.Dispose();
                s2.Dispose();
            }
        }

        [Theory]
        [InlineData(100)]
        public void Writer_ProcessDataFromDistributor_SendResultBack(int count)
        {
            var filename = nameof(Writer_ProcessDataFromDistributor_SendResultBack);
            using (new FileCleaner(filename))
            {
                CreateHashFile(filename, 1);
                CreateConfigFile(countReplics: 1, hash: filename);

                _distributor1.Build();
                _writer1.Build(storageServer1);

                _distributor1.Start();
                _writer1.Start();

                var list = new List<InnerData>();
                for (int i = 1; i < count + 1; i++)
                {
                    var data =
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
                    list.Add(data);
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

        [Theory]
        [InlineData(100)]
        public void ProcessDataFromDistributor_SendResultBack_TwoWriters(int count)
        {
            var filename = nameof(ProcessDataFromDistributor_SendResultBack_TwoWriters);
            using (new FileCleaner(filename))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(countReplics: 1, hash: filename);
                CreateConfigFile(countReplics: 1, hash: filename, filename: config_file2,
                    distrport: storageServer2);

                _distributor1.Build();

                _writer1.Build(storageServer1);
                _writer2.Build(storageServer2, configFile: config_file2);

                _distributor1.Start();

                _writer1.Start();
                _writer2.Start();

                var list = new List<InnerData>();
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

        [Theory]
        [InlineData(100)]
        public void ProcessDataFromDistributor_SendResultBack_TwoWritersAndTwoReplics(int count)
        {
            var filename = nameof(ProcessDataFromDistributor_SendResultBack_TwoWritersAndTwoReplics);
            using (new FileCleaner(filename))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(countReplics: 2, hash: filename);
                
                _distributor1.Build();
                _distributor1.Start();

                CreateConfigFile(countReplics: 1, hash: filename);
                CreateConfigFile(countReplics: 1, hash: filename, filename: config_file2,
                    distrport: storageServer2);

                _writer1.Build(storageServer1);
                _writer2.Build(storageServer2, configFile: config_file2);

                _writer1.Start();
                _writer2.Start();

                var list = new List<InnerData>();
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

        [Theory]
        [InlineData(100)]
        public void ProcessDataFromDistributor_CRUD_TwoWriters(int count)
        {
            var filename = nameof(ProcessDataFromDistributor_CRUD_TwoWriters);
            using (new FileCleaner(filename))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(countReplics: 1, hash: filename);
                CreateConfigFile(countReplics: 1, hash: filename, filename: config_file2,
                    distrport: storageServer2);

                #region hell

                var distributor = DistributorSystem(DistributorCacheConfiguration(20000, 20000),
                    30000);

                _writer1.Build(storageServer1);
                _writer2.Build(storageServer2, configFile: config_file2);

                var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
                var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;

                _proxyTest.Start();

                distributor.Build(new TestInjectionModule());
                distributor.Start();

                _writer1.Start();
                _writer2.Start();

                _proxyTest.Distributor.SayIAmHere(ServerId(distrServer12));

                #endregion

                var api = _proxyTest.CreateApi("Int", false, new IntHashConvertor());

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

                distributor.Dispose();
                _proxyTest.Dispose();
            }
            
        }
    }
}

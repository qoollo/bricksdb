using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Qoollo.Client.Configuration;
using Qoollo.Client.DistributorGate;
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
using Xunit;
using Consts = Qoollo.Impl.Common.Support.Consts;

namespace Qoollo.Tests
{
    [Collection("test collection 1")]
    public class TestRestore : TestBase
    {
        private string file1 = "restoreHelp1.txt";
        private string file2 = "restoreHelp2.txt";
        private string file3 = "restoreHelp3.txt";
        private string file4 = "restoreFile4.txt";

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

                if (servers != null)
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

        [Fact]
        public void Writer_Restore_TwoServers()
        {
            var filename = nameof(Writer_Restore_TwoServers);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            {
                var writer =
                    new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 2, 3,
                        HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                InitInjection.RestoreHelpFileOut = file1;
                _distrTest.Build(1, distrServer1, distrServer12, filename);
                InitInjection.RestoreHelpFileOut = file2;
                _writer1.Build(storageServer1, filename, 1);
                InitInjection.RestoreHelpFileOut = file3;
                _writer2.Build(storageServer2, filename, 1);

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

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                foreach (var data in list)
                {
                    var tr = _distrTest.Main.GetTransactionState(data.Transaction.UserTransaction);
                    if (tr.IsError)
                    {
                        data.Transaction = new Transaction(data.Transaction);
                        data.Transaction.ClearError();
                        _distrTest.Input.ProcessAsync(data);
                    }
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
                var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;

                if (count > 1)
                {
                    Assert.NotEqual(count, mem.Local);
                    Assert.NotEqual(count, mem.Remote);
                }
                Assert.Equal(count, mem.Local + mem.Remote);

                _writer2.Start();
                _writer2.Distributor.Restore(RestoreState.SimpleRestoreNeed);

                Thread.Sleep(TimeSpan.FromMilliseconds(2000));

                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);
                Assert.Equal(count, mem.Local + mem2.Local);
                Assert.Equal(false, _writer1.Restore.IsNeedRestore);
                Assert.Equal(false, _writer2.Restore.IsNeedRestore);

                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
            }            
        }

        [Fact]
        public void Writer_Restore_ThreeServers()
        {
            var filename = nameof(Writer_Restore_ThreeServers);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            using (new FileCleaner(file4))
            {
                var writer =
                new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 3, 3,
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
                var pcc = new ProxyCacheConfiguration(TimeSpan.FromSeconds(3));
                var pccc2 = new ProxyCacheConfiguration(TimeSpan.FromSeconds(4));

                var proxy = new TestProxySystem(new ServerId("localhost", proxyServer),
                    queue, connection, pcc, pccc2, ndrc2,
                    new AsyncTasksConfiguration(new TimeSpan()),
                    new AsyncTasksConfiguration(new TimeSpan()),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));


                InitInjection.RestoreHelpFileOut = file1;
                _distrTest.Build(1, distrServer1, distrServer12, filename);
                InitInjection.RestoreHelpFileOut = file2;
                _writer1.Build(storageServer1, filename, 1);
                InitInjection.RestoreHelpFileOut = file3;
                _writer2.Build(storageServer2, filename, 1);
                InitInjection.RestoreHelpFileOut = file4;
                _writer3.Build(storageServer3, filename, 1);

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
                Assert.Equal(2, counter);

                #endregion

                var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
                var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;
                var mem3 = _writer3.Db.GetDbModules.First() as TestDbInMemory;

                if (count > 1)
                {
                    Assert.NotEqual(count, mem.Local);
                    Assert.NotEqual(count, mem.Remote);
                }
                Assert.Equal(count, mem.Local + mem.Remote);

                _writer2.Start();
                _writer3.Start();

                _writer2.Distributor.Restore(RestoreState.SimpleRestoreNeed);

                Thread.Sleep(TimeSpan.FromMilliseconds(4000));

                _writer3.Distributor.Restore(RestoreState.SimpleRestoreNeed);

                Thread.Sleep(TimeSpan.FromMilliseconds(3000));

                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);
                Assert.Equal(0, mem3.Remote);
                Assert.Equal(count, mem.Local + mem2.Local + mem3.Local);
                Assert.Equal(false, _writer1.Restore.IsNeedRestore);
                Assert.Equal(false, _writer2.Restore.IsNeedRestore);
                Assert.Equal(false, _writer3.Restore.IsNeedRestore);

                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
                _writer3.Dispose();

                proxy.Dispose();
            }
        }

        [Fact]
        public void Writer_RestoreAfterUpdateHashFile_ThreeServers()
        {
            var filename = nameof(Writer_RestoreAfterUpdateHashFile_ThreeServers);
            var filename1 = "1" + nameof(Writer_RestoreAfterUpdateHashFile_ThreeServers);
            var filename2 = "2" + nameof(Writer_RestoreAfterUpdateHashFile_ThreeServers);
            var filename3 = "3" + nameof(Writer_RestoreAfterUpdateHashFile_ThreeServers);
            using (new FileCleaner(filename))
            using (new FileCleaner(filename1))
            using (new FileCleaner(filename2))
            using (new FileCleaner(filename3))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            using (new FileCleaner(file4))
            {
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

                func(filename);
                func(filename1);
                func(filename2);
                func2(filename3);

                InitInjection.RestoreHelpFileOut = file1;
                _distrTest.Build(1, distrServer1, distrServer12, filename);
                InitInjection.RestoreHelpFileOut = file2;
                _writer1.Build(storageServer1, filename1, 1);
                InitInjection.RestoreHelpFileOut = file3;
                _writer2.Build(storageServer2, filename2, 1);
                InitInjection.RestoreHelpFileOut = file4;
                _writer3.Build(storageServer3, filename3, 1);

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
                            Transaction = { Distributor = new ServerId("localhost", distrServer1) }
                        };
                    ev.Transaction.TableName = "Int";

                    list.Add(ev);
                }

                foreach (var data in list)
                {
                    _distrTest.Input.ProcessAsync(data);
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                #endregion

                var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
                var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;
                var mem3 = _writer3.Db.GetDbModules.First() as TestDbInMemory;

                if (count > 1)
                {
                    Assert.NotEqual(count, mem.Local);
                    Assert.NotEqual(count, mem.Remote);
                    Assert.NotEqual(count, mem2.Local);
                    Assert.NotEqual(count, mem2.Remote);
                }
                Assert.Equal(count, mem.Local + mem2.Local);
                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);

                func2(filename);

                _writer3.Start();

                _distrTest.Distributor.UpdateModel();
                _writer1.Distributor.UpdateModel();
                _writer2.Distributor.UpdateModel();

                _writer3.Distributor.Restore(RestoreState.FullRestoreNeed);

                Thread.Sleep(TimeSpan.FromMilliseconds(1400));

                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);
                Assert.Equal(0, mem3.Remote);
                Assert.NotEqual(0, mem3.Local);
                Assert.Equal(count, mem.Local + mem2.Local + mem3.Local);
                Assert.Equal(true, _writer1.Restore.IsNeedRestore);
                Assert.Equal(true, _writer2.Restore.IsNeedRestore);
                Assert.Equal(false, _writer3.Restore.IsNeedRestore);

                _writer1.Dispose();
                _writer2.Dispose();
                _writer3.Dispose();

                _distrTest.Dispose();
            }
        }

        [Fact]
        public void Writer_Restore_SelfRestore()
        {
            var filename = nameof(Writer_Restore_SelfRestore);
            using (new FileCleaner(filename))
            using (new FileCleaner(Impl.Common.Support.Consts.RestoreHelpFile))
            {
                #region hell

                var writer =
                new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 2, 1,
                    HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                var common = new CommonConfiguration(1, 100);

                var storageNet1 = new StorageNetConfiguration("localhost", storageServer1, 157, "testService", 10);
                var storageConfig = new StorageConfiguration(filename, 1, 10, TimeSpan.FromMilliseconds(10000),
                    TimeSpan.FromMilliseconds(200), TimeSpan.FromHours(1), TimeSpan.FromHours(1), false);

                var factory = new TestInMemoryDbFactory();
                var storage1 = new WriterApi(storageNet1, storageConfig, common);

                var distrNet = new DistributorNetConfiguration("localhost",
                    distrServer1, distrServer12, "testService", 10);
                var distrConf = new DistributorConfiguration(1, filename,
                    TimeSpan.FromMilliseconds(10000000), TimeSpan.FromMilliseconds(500000), TimeSpan.FromSeconds(100),
                    TimeSpan.FromMilliseconds(10000000));

                _distr = new DistributorApi(distrNet, distrConf, common);
                _distr.Build();

                #endregion

                _proxy.Start();
                _distr.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer1);

                storage1.Build();
                storage1.AddDbModule(factory);
                storage1.Start();

                Thread.Sleep(500);
                const int count = 50;

                for (int i = 0; i < count; i++)
                {
                    var wait = _proxy.Int.CreateSync(i, i);

                    if (wait.IsError)
                        wait = _proxy.Int.CreateSync(i, i);

                    Assert.Equal(RequestState.Complete, wait.State);
                }

                Assert.Equal(count, factory.Db.Local + factory.Db.Remote);

                writer =
                    new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 1, 1,
                        HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.Save();

                storage1.Api.UpdateModel();
                storage1.Api.Restore(RestoreMode.FullRestoreNeed);

                Thread.Sleep(1000);

                Assert.Equal(count, factory.Db.Local);

                _proxy.Dispose();
                _distr.Dispose();
                storage1.Dispose();
            }
        }

        [Fact]
        public void Writer_Restore_TimeoutDelete()
        {
            var filename = nameof(Writer_Restore_TimeoutDelete);
            using (new FileCleaner(filename))
            using (new FileCleaner(Impl.Common.Support.Consts.RestoreHelpFile))
            {
                #region hell

                var writer =
                new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 1, 1,
                    HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.Save();

                var storageNet1 = new StorageNetConfiguration("localhost", storageServer1, 157, "testService", 10);
                var storageConfig = new StorageConfiguration(filename, 1, 10,
                    TimeSpan.FromMilliseconds(10000),
                    TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(1), TimeSpan.FromSeconds(1), true);

                var factory = new TestInMemoryDbFactory();
                var storage1 = new WriterApi(storageNet1, storageConfig, new CommonConfiguration(1, 10));

                var common = new CommonConfiguration(1, 100);
                var distrNet = new DistributorNetConfiguration("localhost",
                distrServer1, distrServer12, "testService", 10);
                var distrConf = new DistributorConfiguration(1, filename,
                    TimeSpan.FromMilliseconds(10000000), TimeSpan.FromMilliseconds(500000), TimeSpan.FromSeconds(100),
                    TimeSpan.FromMilliseconds(10000000));

                _distr = new DistributorApi(distrNet, distrConf, common);
                _distr.Build();

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

                    Assert.Equal(RequestState.Complete, wait.State);
                }

                Assert.Equal(count, factory.Db.Local);

                for (int i = 0; i < count / 2; i++)
                {
                    var wait = _proxy.Int.DeleteSync(i);

                    Assert.Equal(RequestState.Complete, wait.State);
                }

                Assert.Equal(count / 2, factory.Db.Local);
                Assert.Equal(count / 2, factory.Db.Deleted);

                Thread.Sleep(4000);

                Assert.Equal(count / 2, factory.Db.Local);
                Assert.Equal(0, factory.Db.Deleted);

                _proxy.Dispose();
                _distr.Dispose();
                storage1.Dispose();
            }
        }

        [Fact]
        public void Writer_Restore_ThreeServersTwoReplics()
        {
            var filename = nameof(Writer_Restore_ThreeServersTwoReplics);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            using (new FileCleaner(file4))
            {
                var writer =
                new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 3, 3,
                    HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.SetServer(2, "localhost", storageServer3, 157);
                writer.Save();

                _proxy.Start();
                InitInjection.RestoreHelpFileOut = file1;
                _distrTest.Build(2, distrServer1, distrServer12, filename);
                InitInjection.RestoreHelpFileOut = file2;
                _writer1.Build(storageServer1, filename, 2);
                InitInjection.RestoreHelpFileOut = file3;
                _writer2.Build(storageServer2, filename, 2);
                InitInjection.RestoreHelpFileOut = file4;
                _writer3.Build(storageServer3, filename, 2);

                _distrTest.Start();
                _writer1.Start();
                _writer2.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer12);

                Thread.Sleep(TimeSpan.FromMilliseconds(200));

                const int count = 50;

                var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
                var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;
                var mem3 = _writer3.Db.GetDbModules.First() as TestDbInMemory;

                for (int i = 0; i < count; i++)
                {
                    if (_proxy.Int.CreateSync(i, i).IsError)
                    {
                        _proxy.Int.CreateSync(i, i);
                    }
                }

                if (count > 1)
                {
                    Assert.NotEqual(count, mem.Local);
                    Assert.NotEqual(count, mem.Remote);

                    Assert.NotEqual(count, mem2.Local);
                    Assert.NotEqual(count, mem2.Remote);
                }
                Assert.Equal(count * 2, mem.Local + mem.Remote + mem2.Local + mem2.Remote);

                _writer3.Start();

                _writer3.Distributor.Restore(RestoreState.SimpleRestoreNeed);

                Thread.Sleep(TimeSpan.FromMilliseconds(3000));

                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);
                Assert.Equal(0, mem3.Remote);
                Assert.Equal(count * 2, mem.Local + mem2.Local + mem3.Local);
                Assert.Equal(false, _writer1.Restore.IsNeedRestore);
                Assert.Equal(false, _writer2.Restore.IsNeedRestore);
                Assert.Equal(false, _writer3.Restore.IsNeedRestore);

                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
                _writer3.Dispose();

                _proxy.Dispose();
            }
        }

        [Fact]
        public void Writer_Restore_ThreeServersTwoReplics_UpdateModel()
        {
            var filename = nameof(Writer_Restore_ThreeServersTwoReplics_UpdateModel);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            using (new FileCleaner(file4))
            {
                var writer =
                new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 2, 2,
                    HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                _proxy.Start();
                InitInjection.RestoreHelpFileOut = file1;
                _distrTest.Build(2, distrServer1, distrServer12, filename);
                InitInjection.RestoreHelpFileOut = file2;
                _writer1.Build(storageServer1, filename, 2);
                InitInjection.RestoreHelpFileOut = file3;
                _writer2.Build(storageServer2, filename, 2);
                InitInjection.RestoreHelpFileOut = file4;
                _writer3.Build(storageServer3, filename, 2);

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

                Assert.Equal(count, mem.Local);
                Assert.Equal(0, mem.Remote);

                Assert.Equal(count, mem2.Local);
                Assert.Equal(0, mem2.Remote);

                writer =
                    new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 3, 3,
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

                _writer3.Distributor.Restore(RestoreState.FullRestoreNeed);
                Thread.Sleep(TimeSpan.FromMilliseconds(3000));

                _writer2.Distributor.Restore(RestoreState.FullRestoreNeed);
                Thread.Sleep(TimeSpan.FromMilliseconds(3000));

                _writer1.Distributor.Restore(RestoreState.FullRestoreNeed);
                Thread.Sleep(TimeSpan.FromMilliseconds(3000));

                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);
                Assert.Equal(0, mem3.Remote);
                Assert.Equal(count * 2, mem.Local + mem2.Local + mem3.Local);
                Assert.Equal(false, _writer1.Restore.IsNeedRestore);
                Assert.Equal(false, _writer2.Restore.IsNeedRestore);
                Assert.Equal(false, _writer3.Restore.IsNeedRestore);

                Assert.NotEqual(localLast, mem.Local);
                Assert.NotEqual(localLast2, mem2.Local);

                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
                _writer3.Dispose();

                _proxy.Dispose();
            }
        }

        [Fact]
        public void Distributor_RestoreWithDistributirStateCheck_WithoutModelUpdate()
        {
            var filename = nameof(Distributor_RestoreWithDistributirStateCheck_WithoutModelUpdate);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            {
                var writer =
                new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 2, 3,
                    HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                InitInjection.RestoreHelpFileOut = file1;
                _writer1.Build(storageServer1, filename, 1);
                InitInjection.RestoreHelpFileOut = file2;
                _writer2.Build(storageServer2, filename, 1);

                _proxy.Start();
                _writer1.Start();

                InitInjection.RestoreHelpFileOut = file3;
                _distrTest.Build(1, distrServer1, distrServer12, filename, TimeSpan.FromMilliseconds(100));
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

                Assert.Equal(count, mem.Local + mem.Remote);

                Assert.True(_distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer1).IsAvailable);
                Assert.False(_distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer2).IsAvailable);

                Assert.True(_distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer1).IsServerRestored);
                Assert.False(_distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer2).IsServerRestored);

                Assert.Equal(RestoreState.Restored,
                    _distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer1).RestoreState);

                Assert.Equal(RestoreState.SimpleRestoreNeed,
                    _distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer2).RestoreState);

                _writer2.Start();

                Thread.Sleep(TimeSpan.FromMilliseconds(1200));

                Assert.Equal(RestoreState.SimpleRestoreNeed, _writer2.Distributor.GetRestoreRequiredState());

                _writer2.Distributor.Restore(RestoreState.SimpleRestoreNeed);

                Thread.Sleep(TimeSpan.FromMilliseconds(2000));

                Assert.Equal(RestoreState.Restored, _writer2.Distributor.GetRestoreRequiredState());
                Assert.True(_distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer2).IsServerRestored);

                Assert.Equal(RestoreState.Restored,
                    _distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer2).RestoreState);

                Assert.Equal(count, mem.Local + mem2.Local);

                for (int i = 0; i < count; i++)
                {
                    var result = _proxy.Int.CreateSync(i + 50, i);
                    if (result.IsError)
                    {
                        _proxy.Int.CreateSync(i + 50, i);
                    }
                }

                Assert.Equal(count * 2, mem.Local + mem2.Local);

                _proxy.Dispose();
                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
            }
        }

        [Fact]
        public void Distributor_RestoreWithDistributirStateCheck_WithModelUpdate_RestoreAllServers()
        {
            var filename = nameof(Distributor_RestoreWithDistributirStateCheck_WithModelUpdate_RestoreAllServers);
            var filename2 = nameof(Distributor_RestoreWithDistributirStateCheck_WithModelUpdate_RestoreAllServers)+"2";
            var filename3 = nameof(Distributor_RestoreWithDistributirStateCheck_WithModelUpdate_RestoreAllServers)+"3";
            var filename4 = nameof(Distributor_RestoreWithDistributirStateCheck_WithModelUpdate_RestoreAllServers)+"4";
            using (new FileCleaner(filename))
            using (new FileCleaner(filename2))
            using (new FileCleaner(filename3))
            using (new FileCleaner(filename4))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
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

                func(filename);
                func(filename2);
                func(filename3);
                func2(filename4);

                InitInjection.RestoreHelpFileOut = file1;
                _writer1.Build(storageServer1, filename2, 1);
                InitInjection.RestoreHelpFileOut = file2;
                _writer2.Build(storageServer2, filename3, 1);

                _proxy.Start();
                _writer1.Start();
                _writer2.Start();

                InitInjection.RestoreHelpFileOut = file3;
                _distrTest.Build(1, distrServer1, distrServer12, filename, TimeSpan.FromMilliseconds(1000));
                _distrTest.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer12);

                var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
                var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;

                const int count = 50;
                for (int i = 0; i < count; i++)
                {
                    _proxy.Int.CreateSync(i, i);
                }

                Assert.Equal(count, mem.Local + mem2.Local);
                func2(filename);

                _writer3.Build(storageServer3, filename4, 1);
                _writer3.Start();
                var mem3 = _writer3.Db.GetDbModules.First() as TestDbInMemory;

                _distrTest.Distributor.UpdateModel();

                Thread.Sleep(TimeSpan.FromMilliseconds(1500));

                Assert.Equal(3, _distrTest.WriterSystemModel.Servers.Count);
                Assert.True(_distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer1).IsAvailable);
                Assert.True(_distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer2).IsAvailable);
                Assert.True(_distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer3).IsAvailable);

                Assert.Equal(RestoreState.FullRestoreNeed,
                    _distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer1).RestoreState);
                Assert.Equal(RestoreState.FullRestoreNeed,
                    _distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer2).RestoreState);
                Assert.Equal(RestoreState.FullRestoreNeed,
                    _distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer3).RestoreState);

                Assert.Equal(RestoreState.FullRestoreNeed, _writer1.Distributor.GetRestoreRequiredState());
                Assert.Equal(RestoreState.FullRestoreNeed, _writer2.Distributor.GetRestoreRequiredState());
                Assert.Equal(RestoreState.FullRestoreNeed, _writer3.Distributor.GetRestoreRequiredState());

                _writer1.Distributor.Restore(RestoreState.FullRestoreNeed);
                Thread.Sleep(TimeSpan.FromMilliseconds(1500));
                _writer2.Distributor.Restore(RestoreState.FullRestoreNeed);
                Thread.Sleep(TimeSpan.FromMilliseconds(1500));
                _writer3.Distributor.Restore(RestoreState.FullRestoreNeed);
                Thread.Sleep(TimeSpan.FromMilliseconds(1500));

                Assert.Equal(count, mem.Local + mem2.Local + mem3.Local);

                Assert.Equal(RestoreState.Restored,
                    _distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer1).RestoreState);
                Assert.Equal(RestoreState.Restored,
                    _distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer2).RestoreState);
                Assert.Equal(RestoreState.Restored,
                    _distrTest.WriterSystemModel.Servers.First(x => x.Port == storageServer3).RestoreState);

                Assert.Equal(RestoreState.Restored, _writer1.Distributor.GetRestoreRequiredState());
                Assert.Equal(RestoreState.Restored, _writer2.Distributor.GetRestoreRequiredState());
                Assert.Equal(RestoreState.Restored, _writer3.Distributor.GetRestoreRequiredState());

                _proxy.Dispose();
                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
                _writer3.Dispose();
            }
        }

        [Fact]
        public void Writer_Restore_TwoServer_RestoreFromFile()
        {
            var filename = nameof(Writer_Restore_TwoServer_RestoreFromFile);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            {
                var writer =
                new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 2, 3,
                    HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                _distrTest.Build(1, distrServer1, distrServer12, filename);

                InitInjection.RestoreHelpFileOut = file1;
                _writer1.Build(storageServer1, filename, 1);
                InitInjection.RestoreHelpFileOut = file2;
                CreateRestoreFile(file2, string.Empty, RestoreState.SimpleRestoreNeed,
                    new List<RestoreServerSave>
                    {
                    new RestoreServerSave(new RestoreServer("localhost", storageServer1)
                    {IsFailed = false, IsRestored = false, IsNeedRestore = true})
                    });
                _writer2.Build(storageServer2, filename, 1);

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

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                foreach (var data in list)
                {
                    var tr = _distrTest.Main.GetTransactionState(data.Transaction.UserTransaction);
                    if (tr.IsError)
                    {
                        data.Transaction = new Transaction(data.Transaction);
                        data.Transaction.ClearError();
                        _distrTest.Input.ProcessAsync(data);
                    }
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
                var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;

                if (count > 1)
                {
                    Assert.NotEqual(count, mem.Local);
                    Assert.NotEqual(count, mem.Remote);
                }
                Assert.Equal(count, mem.Local + mem.Remote);

                _writer2.Start();

                Thread.Sleep(TimeSpan.FromMilliseconds(4000));

                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);
                Assert.Equal(count, mem.Local + mem2.Local);
                Assert.Equal(false, _writer1.Restore.IsNeedRestore);
                Assert.Equal(false, _writer2.Restore.IsNeedRestore);

                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
            }
        }

        [Fact]
        public void Writer_Restore_TwoServer_RestoreFromDistributor()
        {
            var filename = nameof(Writer_Restore_TwoServer_RestoreFromDistributor);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            {
                var writer =
               new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 2, 3,
                   HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                _distrTest.Build(1, distrServer1, distrServer12, filename, TimeSpan.FromMilliseconds(100), true);

                InitInjection.RestoreHelpFileOut = file1;
                _writer1.Build(storageServer1, filename, 1);
                InitInjection.RestoreHelpFileOut = file2;
                _writer2.Build(storageServer2, filename, 1);

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

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                foreach (var data in list)
                {
                    var tr = _distrTest.Main.GetTransactionState(data.Transaction.UserTransaction);
                    if (tr.IsError)
                    {
                        data.Transaction = new Transaction(data.Transaction);
                        data.Transaction.ClearError();
                        _distrTest.Input.ProcessAsync(data);
                    }
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
                var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;

                if (count > 1)
                {
                    Assert.NotEqual(count, mem.Local);
                    Assert.NotEqual(count, mem.Remote);
                }
                Assert.Equal(count, mem.Local + mem.Remote);
                Assert.Equal(0, mem2.Local + mem2.Remote);

                _writer2.Start();

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);
                Assert.Equal(count, mem.Local + mem2.Local);
                Assert.Equal(false, _writer1.Restore.IsNeedRestore);
                Assert.Equal(false, _writer2.Restore.IsNeedRestore);

                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
            }
        }

        [Fact]
        public void Writer_Restore_TwoServer_RestoreWithDefaultMode()
        {
            var filename = nameof(Writer_Restore_TwoServer_RestoreWithDefaultMode);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            {
                var writer =
                new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 2, 3,
                    HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                _distrTest.Build(1, distrServer1, distrServer12, filename, TimeSpan.FromMilliseconds(200));

                InitInjection.RestoreHelpFileOut = file1;
                _writer1.Build(storageServer1, filename, 1);
                InitInjection.RestoreHelpFileOut = file2;
                _writer2.Build(storageServer2, filename, 1);

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

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
                var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;

                foreach (var data in list)
                {
                    var tr = _distrTest.Main.GetTransactionState(data.Transaction.UserTransaction);
                    //if (tr.State != TransactionState.Complete)
                    if (tr.IsError)
                    {
                        data.Transaction = new Transaction(data.Transaction);
                        data.Transaction.ClearError();
                        _distrTest.Input.ProcessAsync(data);
                    }
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));                

                if (count > 1)
                {
                    Assert.NotEqual(count, mem.Local);
                    Assert.NotEqual(count, mem.Remote);
                }
                Assert.Equal(count, mem.Local + mem.Remote);

                _writer2.Start();
                Thread.Sleep(TimeSpan.FromMilliseconds(1000));
                _writer2.Distributor.Restore();
                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);
                Assert.Equal(count, mem.Local + mem2.Local);
                Assert.Equal(false, _writer1.Restore.IsNeedRestore);
                Assert.Equal(false, _writer2.Restore.IsNeedRestore);

                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
            }
        }

        [Fact]
        public void Writer_RestoreAfterUpdateHashFile_ThreeServers_RestroeWithDefaultMode()
        {
            var filename = nameof(Writer_RestoreAfterUpdateHashFile_ThreeServers_RestroeWithDefaultMode);
            var filename1 = nameof(Writer_RestoreAfterUpdateHashFile_ThreeServers_RestroeWithDefaultMode)+"1";
            var filename2 = nameof(Writer_RestoreAfterUpdateHashFile_ThreeServers_RestroeWithDefaultMode)+"2";
            var filename3 = nameof(Writer_RestoreAfterUpdateHashFile_ThreeServers_RestroeWithDefaultMode)+"3";
            using (new FileCleaner(filename))
            using (new FileCleaner(filename1))
            using (new FileCleaner(filename2))
            using (new FileCleaner(filename3))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            using (new FileCleaner(file4))
            {
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

                func(filename);
                func(filename1);
                func(filename2);
                func2(filename3);

                InitInjection.RestoreHelpFileOut = file1;
                _distrTest.Build(1, distrServer1, distrServer12, filename, TimeSpan.FromMilliseconds(2000));
                InitInjection.RestoreHelpFileOut = file2;
                _writer1.Build(storageServer1, filename1, 1);
                InitInjection.RestoreHelpFileOut = file3;
                _writer2.Build(storageServer2, filename2, 1);
                InitInjection.RestoreHelpFileOut = file4;
                _writer3.Build(storageServer3, filename3, 1);

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
                            Transaction = { Distributor = new ServerId("localhost", distrServer1) }
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
                    Assert.NotEqual(count, mem.Local);
                    Assert.NotEqual(count, mem.Remote);
                    Assert.NotEqual(count, mem2.Local);
                    Assert.NotEqual(count, mem2.Remote);
                }
                Assert.Equal(count, mem.Local + mem2.Local);
                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);

                func2(filename);

                _writer3.Start();

                _distrTest.Distributor.UpdateModel();
                _writer1.Distributor.UpdateModel();
                _writer2.Distributor.UpdateModel();

                Thread.Sleep(TimeSpan.FromMilliseconds(3000));
                _writer3.Distributor.Restore();
                Thread.Sleep(TimeSpan.FromMilliseconds(1400));


                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);
                Assert.Equal(0, mem3.Remote);
                Assert.NotEqual(0, mem3.Local);
                Assert.Equal(count, mem.Local + mem2.Local + mem3.Local);
                Assert.Equal(true, _writer1.Restore.IsNeedRestore);
                Assert.Equal(true, _writer2.Restore.IsNeedRestore);
                Assert.Equal(false, _writer3.Restore.IsNeedRestore);

                _writer1.Dispose();
                _writer2.Dispose();
                _writer3.Dispose();

                _distrTest.Dispose();
            }
        }

        [Fact]
        public void Writer_Restore_ThreeServers_DirectServersForRestore()
        {
            var filename = nameof(Writer_Restore_ThreeServers_DirectServersForRestore);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            using (new FileCleaner(file4))
            {
                var writer =
                new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 3, 3,
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
                var pcc = new ProxyCacheConfiguration(TimeSpan.FromSeconds(3));
                var pccc2 = new ProxyCacheConfiguration(TimeSpan.FromSeconds(3));

                var proxy = new TestProxySystem(new ServerId("localhost", proxyServer),
                    queue, connection, pcc, pccc2, ndrc2,
                    new AsyncTasksConfiguration(new TimeSpan()),
                    new AsyncTasksConfiguration(new TimeSpan()),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));

                InitInjection.RestoreHelpFileOut = file1;
                _distrTest.Build(1, distrServer1, distrServer12, filename);
                InitInjection.RestoreHelpFileOut = file2;
                _writer1.Build(storageServer1, filename, 1);
                InitInjection.RestoreHelpFileOut = file3;
                _writer2.Build(storageServer2, filename, 1);
                InitInjection.RestoreHelpFileOut = file4;
                _writer3.Build(storageServer3, filename, 1);

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

                var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
                var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;
                var mem3 = _writer3.Db.GetDbModules.First() as TestDbInMemory;

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
                Assert.Equal(2, counter);

                #endregion                

                if (count > 1)
                {
                    Assert.NotEqual(count, mem.Local);
                    Assert.NotEqual(count, mem.Remote);
                }
                Assert.Equal(count, mem.Local + mem.Remote);

                _writer2.Start();
                _writer3.Start();

                _writer2.Distributor.Restore(new List<ServerId> { new ServerId("localhost", storageServer1) },
                    RestoreState.SimpleRestoreNeed);
                Thread.Sleep(TimeSpan.FromMilliseconds(4000));

                _writer3.Distributor.Restore(new List<ServerId> { new ServerId("localhost", storageServer1) },
                    RestoreState.SimpleRestoreNeed);
                Thread.Sleep(TimeSpan.FromMilliseconds(3000));

                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);
                Assert.Equal(0, mem3.Remote);
                Assert.Equal(count, mem.Local + mem2.Local + mem3.Local);
                Assert.Equal(false, _writer1.Restore.IsNeedRestore);
                Assert.Equal(RestoreState.SimpleRestoreNeed, _writer2.Restore.RestoreState);
                Assert.Equal(RestoreState.SimpleRestoreNeed, _writer3.Restore.RestoreState);

                _writer2.Distributor.Restore(new List<ServerId> { new ServerId("localhost", storageServer3) },
                    RestoreState.SimpleRestoreNeed);
                Thread.Sleep(TimeSpan.FromMilliseconds(4000));

                _writer3.Distributor.Restore(new List<ServerId> { new ServerId("localhost", storageServer2) },
                    RestoreState.SimpleRestoreNeed);
                Thread.Sleep(TimeSpan.FromMilliseconds(3000));

                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);
                Assert.Equal(0, mem3.Remote);
                Assert.Equal(count, mem.Local + mem2.Local + mem3.Local);
                Assert.Equal(false, _writer1.Restore.IsNeedRestore);
                Assert.Equal(false, _writer2.Restore.IsNeedRestore);
                Assert.Equal(false, _writer3.Restore.IsNeedRestore);

                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
                _writer3.Dispose();

                proxy.Dispose();
            }
        }

        [Fact]
        public void Writer_Restore_TwoServer_RestoreFromDistributor_EnableCommand()
        {
            var filename = nameof(Writer_Restore_TwoServer_RestoreFromDistributor_EnableCommand);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            using (new FileCleaner(file4))
            {
                var writer =
                new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 2, 3,
                    HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                _distrTest.Build(1, distrServer1, distrServer12, filename, TimeSpan.FromMilliseconds(100), true);

                InitInjection.RestoreHelpFileOut = file1;
                _writer1.Build(storageServer1, filename, 1);
                InitInjection.RestoreHelpFileOut = file2;
                _writer2.Build(storageServer2, filename, 1);

                _distrTest.Start();
                _distrTest.Distributor.AutoRestoreSetMode(false);

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

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                foreach (var data in list)
                {
                    var tr = _distrTest.Main.GetTransactionState(data.Transaction.UserTransaction);
                    if (tr.IsError)
                    {
                        data.Transaction = new Transaction(data.Transaction);
                        data.Transaction.ClearError();
                        _distrTest.Input.ProcessAsync(data);
                    }
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
                var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;

                if (count > 1)
                {
                    Assert.NotEqual(count, mem.Local);
                    Assert.NotEqual(count, mem.Remote);
                }
                Assert.Equal(count, mem.Local + mem.Remote);
                Assert.Equal(0, mem2.Local + mem2.Remote);

                _writer2.Start();

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                Assert.Equal(count, mem.Local + mem.Remote);
                Assert.Equal(0, mem2.Local + mem2.Remote);
                Assert.Equal(true, _writer2.Restore.IsNeedRestore);

                _distrTest.Distributor.AutoRestoreSetMode(true);

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);
                Assert.Equal(count, mem.Local + mem2.Local);
                Assert.Equal(false, _writer1.Restore.IsNeedRestore);
                Assert.Equal(false, _writer2.Restore.IsNeedRestore);

                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
            }
        }

        [Fact]
        public void Writer_Restore_TwoServer_RestoreFromDistributorWithCommand()
        {
            var filename = nameof(Writer_Restore_TwoServer_RestoreFromDistributorWithCommand);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            using (new FileCleaner(file4))
            {
                var writer =
                new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 2, 3,
                    HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                _distrTest.Build(1, distrServer1, distrServer12, filename, TimeSpan.FromMilliseconds(100));

                InitInjection.RestoreHelpFileOut = file1;
                _writer1.Build(storageServer1, filename, 1);
                InitInjection.RestoreHelpFileOut = file2;
                _writer2.Build(storageServer2, filename, 1);

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

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                foreach (var data in list)
                {
                    var tr = _distrTest.Main.GetTransactionState(data.Transaction.UserTransaction);
                    if (tr.IsError)
                    {
                        data.Transaction = new Transaction(data.Transaction);
                        data.Transaction.ClearError();
                        _distrTest.Input.ProcessAsync(data);
                    }
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
                var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;

                if (count > 1)
                {
                    Assert.NotEqual(count, mem.Local);
                    Assert.NotEqual(count, mem.Remote);
                }
                Assert.Equal(count, mem.Local + mem.Remote);
                Assert.Equal(0, mem2.Local + mem2.Remote);

                _writer2.Start();
                _distrTest.Distributor.Restore(new ServerId("localhost", storageServer2),
                    new ServerId("localhost", storageServer1), RestoreState.SimpleRestoreNeed);

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);
                Assert.Equal(count, mem.Local + mem2.Local);
                Assert.Equal(false, _writer1.Restore.IsNeedRestore);
                Assert.Equal(false, _writer2.Restore.IsNeedRestore);

                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
            }
        }

        [Fact]
        public void Writer_Restore_TwoServers_Package()
        {
            var filename = nameof(Writer_Restore_TwoServers_Package);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            {
                var writer =
                new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 2, 3,
                    HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                InitInjection.RestoreUsePackage = true;
                InitInjection.RestoreHelpFileOut = file1;
                _distrTest.Build(1, distrServer1, distrServer12, filename);
                InitInjection.RestoreHelpFileOut = file2;
                _writer1.Build(storageServer1, filename, 1);
                InitInjection.RestoreHelpFileOut = file3;
                _writer2.Build(storageServer2, filename, 1);

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

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                foreach (var data in list)
                {
                    var tr = _distrTest.Main.GetTransactionState(data.Transaction.UserTransaction);
                    if (tr.IsError)
                    {
                        data.Transaction = new Transaction(data.Transaction);
                        data.Transaction.ClearError();
                        _distrTest.Input.ProcessAsync(data);
                    }
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
                var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;

                if (count > 1)
                {
                    Assert.NotEqual(count, mem.Local);
                    Assert.NotEqual(count, mem.Remote);
                }
                Assert.Equal(count, mem.Local + mem.Remote);

                _writer2.Start();
                _writer2.Distributor.Restore(RestoreState.SimpleRestoreNeed);

                Thread.Sleep(TimeSpan.FromMilliseconds(2000));

                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);
                Assert.Equal(count, mem.Local + mem2.Local);
                Assert.Equal(false, _writer1.Restore.IsNeedRestore);
                Assert.Equal(false, _writer2.Restore.IsNeedRestore);

                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
            }
        }
    }
}

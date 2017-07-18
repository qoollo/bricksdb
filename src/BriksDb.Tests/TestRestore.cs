using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Qoollo.Client.Request;
using Qoollo.Client.Support;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.TestSupport;
using Qoollo.Tests.NetMock;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestWriter;
using Xunit;
using Consts = Qoollo.Impl.Common.Support.Consts;

namespace Qoollo.Tests
{
    [Collection("test collection 1")]
    public class TestRestore : TestBase
    {
        private void CreateRestoreFile(string filename, string tableName, RestoreState state,
            List<RestoreServerSave> servers = null)
        {
            using (var writer = new StreamWriter(filename))
            {
                writer.WriteLine("<?xml version=\"1.0\"?>");
                writer.WriteLine(
                    "<RestoreSaveHelper xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" RestoreState=\"{0}\">", 
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

        private InnerData InnerData(int i)
        {
            var ev = new InnerData(new Transaction(
                HashConvertor.GetString(i.ToString(CultureInfo.InvariantCulture)), "default")
            {
                OperationName = OperationName.Create,
                OperationType = OperationType.Async
            })
            {
                Data = CommonDataSerializer.Serialize(i),
                Key = CommonDataSerializer.Serialize(i),
                Transaction = {Distributor = new ServerId("localhost", distrServer1)}
            };
            ev.Transaction.TableName = "Int";
            return ev;
        }

        [Theory]
        [InlineData(50)]
        public void Writer_Restore_TwoServers(int count)
        {
            var filename = nameof(Writer_Restore_TwoServers);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename);

                //distrServer1, distrServer12
                InitInjection.RestoreHelpFileOut = file1;
                _distrTest.Build();
                InitInjection.RestoreHelpFileOut = file2;
                _writer1.Build(storageServer1);
                InitInjection.RestoreHelpFileOut = file3;
                _writer2.Build(storageServer2);

                _distrTest.Start();
                _writer1.Start();

                var list = new List<InnerData>();
                for (int i = 1; i < count + 1; i++)
                {
                    var data = InnerData(i);
                    data.Transaction.Distributor = new ServerId("localhost", distrServer1);

                    list.Add(data);

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

        [Theory]
        [InlineData(50)]
        public void Writer_Restore_ThreeServers(int count)
        {
            var filename = nameof(Writer_Restore_ThreeServers);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            using (new FileCleaner(file4))
            {
                CreateHashFile(filename, 3);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename);

                var proxy = TestProxySystem(proxyServer, 3, 4);

                //distrServer1, distrServer12
                InitInjection.RestoreHelpFileOut = file1;
                _distrTest.Build();
                InitInjection.RestoreHelpFileOut = file2;
                _writer1.Build(storageServer1);
                InitInjection.RestoreHelpFileOut = file3;
                _writer2.Build(storageServer2);
                InitInjection.RestoreHelpFileOut = file4;
                _writer3.Build(storageServer3);

                #region hell2

                proxy.Build(new TestInjectionModule());
                proxy.Start();

                _distrTest.Start();
                _writer1.Start();

                proxy.Distributor.SayIAmHere(ServerId(distrServer12));

                Thread.Sleep(TimeSpan.FromMilliseconds(200));

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

                Thread.Sleep(TimeSpan.FromMilliseconds(8000));

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

        [Theory]
        [InlineData(50)]
        public void Writer_RestoreAfterUpdateHashFile_ThreeServers(int count)
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
                #region hell

                CreateHashFile(filename, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename, filename: config_file);

                CreateHashFile(filename1, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename1, filename: config_file1);

                CreateHashFile(filename2, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename2, filename: config_file2);

                CreateHashFile(filename3, 3);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename3, filename: config_file3);

                //distrServer1, distrServer12, 
                InitInjection.RestoreHelpFileOut = file1;
                _distrTest.Build(configFile: config_file);
                InitInjection.RestoreHelpFileOut = file2;
                _writer1.Build(storageServer1, configFile: config_file1);
                InitInjection.RestoreHelpFileOut = file3;
                _writer2.Build(storageServer2, configFile: config_file2);
                InitInjection.RestoreHelpFileOut = file4;
                _writer3.Build(storageServer3, configFile: config_file3);

                _distrTest.Start();
                _writer1.Start();
                _writer2.Start();

                var list = new List<InnerData>();
                for (int i = 1; i < count + 1; i++)
                {
                    var data = InnerData(i);
                    data.Transaction.Distributor = ServerId(distrServer1);

                    list.Add(data);

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

                CreateHashFile(filename, 3);

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

        [Theory]
        [InlineData(50)]
        public void Writer_Restore_SelfRestore(int count)
        {
            var filename = nameof(Writer_Restore_SelfRestore);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename);

                //, distrServer1, distrServer12
                var factory = new TestInMemoryDbFactory(_kernel);
                var storage1 = WriterApi(StorageConfiguration(filename, 1, 200), storageServer1);
                var distr = DistributorApi(DistributorConfiguration(filename, 1));

                distr.Module = new TestInjectionModule();
                distr.Build();

                _proxy.Start();
                distr.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer1);

                storage1.Module = new TestInjectionModule();
                storage1.Build();
                storage1.AddDbModule(factory);
                storage1.Start();

                Thread.Sleep(500);

                for (int i = 0; i < count; i++)
                {
                    var wait = _proxy.Int.CreateSync(i, i);

                    if (wait.IsError)
                        wait = _proxy.Int.CreateSync(i, i);

                    Assert.Equal(RequestState.Complete, wait.State);
                }

                Assert.Equal(count, factory.Db.Local + factory.Db.Remote);

                CreateHashFile(filename, 1);

                storage1.Api.UpdateModel();
                storage1.Api.Restore(RestoreMode.FullRestoreNeed);

                Thread.Sleep(1000);

                Assert.Equal(count, factory.Db.Local);

                _proxy.Dispose();
                distr.Dispose();
                storage1.Dispose();
            }
        }

        [Theory]
        [InlineData(50)]
        public void Writer_Restore_TimeoutDelete(int count)
        {
            var filename = nameof(Writer_Restore_TimeoutDelete);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                CreateHashFile(filename, 1);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename);

                var factory = new TestInMemoryDbFactory(_kernel);
                var storage1 = WriterApi(StorageConfiguration(filename, 1, 200, 1, 60, true), storageServer1);

                //, distrServer1, distrServer12
                var distr = DistributorApi(DistributorConfiguration(filename, 1));
                distr.Module = new TestInjectionModule();
                distr.Build();

                _proxy.Start();
                distr.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer1);

                storage1.Module = new TestInjectionModule();
                storage1.Build();
                storage1.AddDbModule(factory);
                storage1.Start();

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
                distr.Dispose();
                storage1.Dispose();
            }
        }

        [Theory]
        [InlineData(50)]
        public void Writer_Restore_ThreeServersTwoReplics(int count)
        {
            var filename = nameof(Writer_Restore_ThreeServersTwoReplics);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            using (new FileCleaner(file4))
            {
                CreateHashFile(filename, 3);
                CreateConfigFile(countReplics: 2, hash: filename);

                //distrServer1, distrServer12
                _proxy.Start();
                InitInjection.RestoreHelpFileOut = file1;
                _distrTest.Build();
                InitInjection.RestoreHelpFileOut = file2;
                _writer1.Build(storageServer1);
                InitInjection.RestoreHelpFileOut = file3;
                _writer2.Build(storageServer2);
                InitInjection.RestoreHelpFileOut = file4;
                _writer3.Build(storageServer3);

                _distrTest.Start();
                _writer1.Start();
                _writer2.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer12);

                Thread.Sleep(TimeSpan.FromMilliseconds(200));

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

        [Theory]
        [InlineData(50)]
        public void Writer_Restore_ThreeServersTwoReplics_UpdateModel(int count)
        {
            var filename = nameof(Writer_Restore_ThreeServersTwoReplics_UpdateModel);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            using (new FileCleaner(file4))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(countReplics: 2, hash: filename);

                //distrServer1, distrServer12
                _proxy.Start();
                InitInjection.RestoreHelpFileOut = file1;
                _distrTest.Build();
                InitInjection.RestoreHelpFileOut = file2;
                _writer1.Build(storageServer1);
                InitInjection.RestoreHelpFileOut = file3;
                _writer2.Build(storageServer2);
                InitInjection.RestoreHelpFileOut = file4;
                _writer3.Build(storageServer3);

                _distrTest.Start();
                _writer1.Start();
                _writer2.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer12);

                Thread.Sleep(TimeSpan.FromMilliseconds(200));

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

                CreateHashFile(filename, 3);

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

        [Theory]
        [InlineData(50)]
        public void Distributor_RestoreWithDistributirStateCheck_WithoutModelUpdate(int count)
        {
            var filename = nameof(Distributor_RestoreWithDistributirStateCheck_WithoutModelUpdate);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename);

                InitInjection.RestoreHelpFileOut = file1;
                _writer1.Build(storageServer1);
                InitInjection.RestoreHelpFileOut = file2;
                _writer2.Build(storageServer2);

                _proxy.Start();
                _writer1.Start();

                //distrServer1, distrServer12, 
                InitInjection.RestoreHelpFileOut = file3;
                _distrTest.Build(TimeSpan.FromMilliseconds(100));
                _distrTest.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer12);

                var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
                var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;

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

        [Theory]
        [InlineData(50)]
        public void Distributor_RestoreWithDistributirStateCheck_WithModelUpdate_RestoreAllServers(int count)
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
                CreateHashFile(filename, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename, filename: config_file);

                CreateHashFile(filename2, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename2, filename: config_file2);

                CreateHashFile(filename3, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename3, filename: config_file3);

                CreateHashFile(filename4, 3);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename4, filename: config_file4);

                InitInjection.RestoreHelpFileOut = file1;
                _writer1.Build(storageServer1, configFile: config_file2);
                InitInjection.RestoreHelpFileOut = file2;
                _writer2.Build(storageServer2, configFile: config_file3);

                _proxy.Start();
                _writer1.Start();
                _writer2.Start();

                //distrServer1, distrServer12, 
                InitInjection.RestoreHelpFileOut = file3;
                _distrTest.Build(TimeSpan.FromMilliseconds(1000), configFile: config_file);
                _distrTest.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer12);

                var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
                var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;

                for (int i = 0; i < count; i++)
                {
                    _proxy.Int.CreateSync(i, i);
                }

                Assert.Equal(count, mem.Local + mem2.Local);
                CreateHashFile(filename, 3);

                _writer3.Build(storageServer3, configFile: config_file4);
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

        [Theory]
        [InlineData(50)]
        public void Writer_Restore_TwoServer_RestoreFromFile(int count)
        {
            var filename = nameof(Writer_Restore_TwoServer_RestoreFromFile);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename);

                //distrServer1, distrServer12
                _distrTest.Build();

                InitInjection.RestoreHelpFileOut = file1;
                _writer1.Build(storageServer1);
                InitInjection.RestoreHelpFileOut = file2;
                CreateRestoreFile(file2, string.Empty, RestoreState.SimpleRestoreNeed,
                    new List<RestoreServerSave>
                    {
                        new RestoreServerSave(new RestoreServer("localhost", storageServer1)
                        {IsFailed = false, IsRestored = false, IsNeedRestore = true})
                    });
                _writer2.Build(storageServer2);

                _distrTest.Start();
                _writer1.Start();

                var list = new List<InnerData>();
                for (int i = 1; i < count + 1; i++)
                {
                    var data = InnerData(i);
                    data.Transaction.Distributor = ServerId(distrServer1);

                    list.Add(data);

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

        [Theory]
        [InlineData(50)]
        public void Writer_Restore_TwoServer_RestoreFromDistributor(int count)
        {
            var filename = nameof(Writer_Restore_TwoServer_RestoreFromDistributor);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename);

                //distrServer1, distrServer12, 
                _distrTest.Build(TimeSpan.FromMilliseconds(100), true);

                InitInjection.RestoreHelpFileOut = file1;
                _writer1.Build(storageServer1);
                InitInjection.RestoreHelpFileOut = file2;
                _writer2.Build(storageServer2);

                _distrTest.Start();
                _writer1.Start();

                var list = new List<InnerData>();
                for (int i = 1; i < count + 1; i++)
                {
                    var data = InnerData(i);
                    data.Transaction.Distributor = ServerId(distrServer1);

                    list.Add(data);

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

        [Theory]
        [InlineData(50)]
        public void Writer_Restore_TwoServer_RestoreWithDefaultMode(int count)
        {
            var filename = nameof(Writer_Restore_TwoServer_RestoreWithDefaultMode);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename);

                //distrServer1, distrServer12, 
                _distrTest.Build(TimeSpan.FromMilliseconds(200));

                InitInjection.RestoreHelpFileOut = file1;
                _writer1.Build(storageServer1);
                InitInjection.RestoreHelpFileOut = file2;
                _writer2.Build(storageServer2);

                _distrTest.Start();
                _writer1.Start();

                var list = new List<InnerData>();
                for (int i = 1; i < count + 1; i++)
                {
                    var data = InnerData(i);
                    data.Transaction.Distributor = ServerId(distrServer1);

                    list.Add(data);

                    _distrTest.Input.ProcessAsync(data);
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
                var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;

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

        [Theory]
        [InlineData(50)]
        public void Writer_RestoreAfterUpdateHashFile_ThreeServers_RestroeWithDefaultMode(int count)
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
                CreateHashFile(filename, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename, filename: config_file);

                CreateHashFile(filename1, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename1, filename: config_file1);

                CreateHashFile(filename2, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename2, filename: config_file2);

                CreateHashFile(filename3, 3);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename3, filename: config_file3);

                //distrServer1, distrServer12, 
                InitInjection.RestoreHelpFileOut = file1;
                _distrTest.Build(TimeSpan.FromMilliseconds(2000), configFile: config_file);
                InitInjection.RestoreHelpFileOut = file2;
                _writer1.Build(storageServer1, configFile: config_file1);
                InitInjection.RestoreHelpFileOut = file3;
                _writer2.Build(storageServer2, configFile: config_file2);
                InitInjection.RestoreHelpFileOut = file4;
                _writer3.Build(storageServer3, configFile: config_file3);

                _distrTest.Start();
                _writer1.Start();
                _writer2.Start();

                var list = new List<InnerData>();
                for (int i = 1; i < count + 1; i++)
                {
                    var data = InnerData(i);
                    data.Transaction.Distributor = ServerId(distrServer1);

                    list.Add(data);

                    _distrTest.Input.ProcessAsync(data);
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

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

                CreateHashFile(filename, 3);

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

        [Theory]
        [InlineData(50)]
        public void Writer_Restore_ThreeServers_DirectServersForRestore(int count)
        {
            var filename = nameof(Writer_Restore_ThreeServers_DirectServersForRestore);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            using (new FileCleaner(file4))
            {
                CreateHashFile(filename, 3);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename);

                var proxy = TestProxySystem(proxyServer, 3, 3);

                //distrServer1, distrServer12
                InitInjection.RestoreHelpFileOut = file1;
                _distrTest.Build();
                InitInjection.RestoreHelpFileOut = file2;
                _writer1.Build(storageServer1);
                InitInjection.RestoreHelpFileOut = file3;
                _writer2.Build(storageServer2);
                InitInjection.RestoreHelpFileOut = file4;
                _writer3.Build(storageServer3);

                #region hell2

                proxy.Build(new TestInjectionModule());
                proxy.Start();

                _distrTest.Start();
                _writer1.Start();

                proxy.Distributor.SayIAmHere(ServerId(distrServer12));

                Thread.Sleep(TimeSpan.FromMilliseconds(200));

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
                //Assert.Equal(RestoreState.SimpleRestoreNeed, _writer2.Restore.RestoreState);
                //Assert.Equal(RestoreState.SimpleRestoreNeed, _writer3.Restore.RestoreState);

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

        [Theory]
        [InlineData(50)]
        public void Writer_Restore_TwoServer_RestoreFromDistributor_EnableCommand(int count)
        {
            var filename = nameof(Writer_Restore_TwoServer_RestoreFromDistributor_EnableCommand);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            using (new FileCleaner(file4))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename);

                //distrServer1, distrServer12, 
                _distrTest.Build(TimeSpan.FromMilliseconds(100), true);

                InitInjection.RestoreHelpFileOut = file1;
                _writer1.Build(storageServer1);
                InitInjection.RestoreHelpFileOut = file2;
                _writer2.Build(storageServer2);

                _distrTest.Start();
                _distrTest.Distributor.AutoRestoreSetMode(false);

                _writer1.Start();

                var list = new List<InnerData>();
                for (int i = 1; i < count + 1; i++)
                {
                    var data = InnerData(i);
                    data.Transaction.Distributor = ServerId(distrServer1);

                    list.Add(data);

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

        [Theory]
        [InlineData(50)]
        public void Writer_Restore_TwoServer_RestoreFromDistributorWithCommand(int count)
        {
            var filename = nameof(Writer_Restore_TwoServer_RestoreFromDistributorWithCommand);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            using (new FileCleaner(file4))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename);

                //distrServer1, distrServer12, 
                _distrTest.Build(TimeSpan.FromMilliseconds(100));

                InitInjection.RestoreHelpFileOut = file1;
                _writer1.Build(storageServer1);
                InitInjection.RestoreHelpFileOut = file2;
                _writer2.Build(storageServer2);

                _distrTest.Start();
                _writer1.Start();

                var list = new List<InnerData>();
                for (int i = 1; i < count + 1; i++)
                {
                    var data = InnerData(i);
                    data.Transaction.Distributor = ServerId(distrServer1);

                    list.Add(data);

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
                //Assert.Equal(false, _writer1.Restore.IsNeedRestore);
                //Assert.Equal(false, _writer2.Restore.IsNeedRestore);

                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
            }
        }

        [Theory]
        [InlineData(50)]
        public void Writer_Restore_TwoServers_Package(int count)
        {
            var filename = nameof(Writer_Restore_TwoServers_Package);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename);

                //distrServer1, distrServer12
                InitInjection.RestoreUsePackage = true;
                InitInjection.RestoreHelpFileOut = file1;
                _distrTest.Build();
                InitInjection.RestoreHelpFileOut = file2;
                _writer1.Build(storageServer1);
                InitInjection.RestoreHelpFileOut = file3;
                _writer2.Build(storageServer2);

                _distrTest.Start();
                _writer1.Start();

                var list = new List<InnerData>();
                for (int i = 1; i < count + 1; i++)
                {
                    var data = InnerData(i);
                    data.Transaction.Distributor = ServerId(distrServer1);

                    list.Add(data);

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

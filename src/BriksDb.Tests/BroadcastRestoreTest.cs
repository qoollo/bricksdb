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
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.TestSupport;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestWriter;
using Xunit;

namespace Qoollo.Tests
{
    public class BroadcastRestoreTest : TestBase
    {
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
        [InlineData(50, false)]
        [InlineData(50, true)]
        public void Writer_SimpleRestore_TwoServers(int count, bool packageRestore)
        {
            var filename = nameof(Writer_SimpleRestore_TwoServers);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(distrthreads: 2, countReplics: 1, hash: filename);
                CreateConfigFile(distrthreads: 2, countReplics: 1, hash: filename, 
                    filename: config_file2, distrport: storageServer2);

                InitInjection.RestoreUsePackage = packageRestore;
                _distrTest.Build();

                InitInjection.RestoreHelpFileOut = file1;
                _writer1.Build(storageServer1);

                InitInjection.RestoreHelpFileOut = file2;
                _writer2.Build(storageServer2, configFile: config_file2);

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
                _writer1.Distributor.Restore(RestoreState.SimpleRestoreNeed, RestoreType.Broadcast);

                Thread.Sleep(TimeSpan.FromMilliseconds(2000));

                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);
                Assert.Equal(count, mem.Local + mem2.Local);

                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
            }
        }

        [Theory]
        [InlineData(50, false)]
        [InlineData(50, true)]
        public void Writer_SimpleRestore_ThreeServers_OneBroadcast(int count, bool packageRestore)
        {
            var filename = nameof(Writer_SimpleRestore_ThreeServers_OneBroadcast);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            {
                CreateHashFile(filename, 3);
                CreateConfigFile(distrthreads: 2, countReplics: 1, hash: filename);
                CreateConfigFile(distrthreads: 2, countReplics: 1, hash: filename,
                    filename: config_file2, distrport: storageServer2);

                CreateConfigFile(distrthreads: 2, countReplics: 1, hash: filename,
                    filename: config_file3, distrport: storageServer3);

                InitInjection.RestoreUsePackage = packageRestore;
                
                _distrTest.Build();

                InitInjection.RestoreHelpFileOut = file1;
                _writer1.Build(storageServer1, filename);

                InitInjection.RestoreHelpFileOut = file2;
                _writer2.Build(storageServer2, filename, configFile: config_file2);

                InitInjection.RestoreHelpFileOut = file3;
                _writer3.Build(storageServer3, filename, configFile: config_file3);

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
                var mem3 = _writer3.Db.GetDbModules.First() as TestDbInMemory;

                if (count > 1)
                {
                    Assert.NotEqual(count, mem.Local);
                    Assert.NotEqual(count, mem.Remote);
                }
                Assert.Equal(count, mem.Local + mem.Remote);

                _writer2.Start();
                _writer3.Start();
                _writer1.Distributor.Restore(RestoreState.SimpleRestoreNeed, RestoreType.Broadcast);

                Thread.Sleep(TimeSpan.FromMilliseconds(2000));

                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);
                Assert.Equal(0, mem3.Remote);
                Assert.Equal(count, mem.Local + mem2.Local + mem3.Local);

                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
                _writer3.Dispose();
            }
        }

        [Theory]
        [InlineData(50, 1, false)]
        [InlineData(50, 1, true)]
        [InlineData(50, 2, false)]        
        [InlineData(50, 2, true)]
        public void Writer_SimpleRestore_ThreeServers_TwoBroadcast(int count, int replics, bool packageRestore)
        {
            var filename = nameof(Writer_SimpleRestore_ThreeServers_TwoBroadcast);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            {
                CreateHashFile(filename, 3);
                CreateConfigFile(countReplics: replics, hash: filename);
                CreateConfigFile(distrthreads: 2, countReplics: replics, hash: filename,
                    filename: config_file2, distrport: storageServer2);
                CreateConfigFile(distrthreads: 2, countReplics: replics, hash: filename,
                    filename: config_file3, distrport: storageServer3);

                InitInjection.RestoreUsePackage = packageRestore;

                _distrTest.Build();

                InitInjection.RestoreHelpFileOut = file1;
                _writer1.Build(storageServer1, "w1");

                InitInjection.RestoreHelpFileOut = file2;
                _writer2.Build(storageServer2, "w2", config_file2);

                InitInjection.RestoreHelpFileOut = file3;
                _writer3.Build(storageServer3, "w3", config_file3);

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
                var mem3 = _writer3.Db.GetDbModules.First() as TestDbInMemory;

                Assert.Equal(count*replics, mem.Local + mem.Remote + mem2.Local + mem2.Remote);

                _writer3.Start();

                _writer2.Distributor.Restore(RestoreState.SimpleRestoreNeed, RestoreType.Broadcast);
                _writer1.Distributor.Restore(RestoreState.SimpleRestoreNeed, RestoreType.Broadcast);

                Thread.Sleep(TimeSpan.FromMilliseconds(2000));

                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);
                Assert.Equal(count*replics, mem.Local + mem2.Local + mem3.Local);

                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
                _writer3.Dispose();
            }
        }

        [Theory]
        [InlineData(50, false)]
        [InlineData(50, true)]
        public void Writer_FullRestore_TwoServers(int count, bool packageRestore)
        {
            var filename = nameof(Writer_FullRestore_TwoServers);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            {
                CreateHashFile(filename, 1);
                CreateConfigFile(distrthreads: 2, countReplics: 1, hash: filename);

                CreateConfigFile(distrthreads: 2, countReplics: 1, hash: filename,
                    filename: config_file2, distrport: storageServer2);

                InitInjection.RestoreUsePackage = packageRestore;

                _distrTest.Build();

                InitInjection.RestoreHelpFileOut = file1;
                _writer1.Build(storageServer1);

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
                Assert.Equal(count, mem.Local + mem.Remote);

                CreateHashFile(filename, 2);

                InitInjection.RestoreHelpFileOut = file2;
                _writer2.Build(storageServer2, configFile: config_file2);
                _writer2.Start();

                var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;

                _writer1.Distributor.UpdateModel();
                _writer1.Distributor.Restore(RestoreState.FullRestoreNeed, RestoreType.Broadcast);

                Thread.Sleep(TimeSpan.FromMilliseconds(2000));

                Assert.NotEqual(0, mem2.Local);
                Assert.Equal(count, mem.Local + mem2.Local);

                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
            }
        }

        [Theory]
        [InlineData(50, 1, false)]
        [InlineData(50, 1, true)]
        [InlineData(50, 2, false)]
        [InlineData(50, 2, true)]
        public void Writer_FullRestore_ThreeServers(int count, int replics, bool packageRestore)
        {
            var filename = nameof(Writer_FullRestore_ThreeServers);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(countReplics: replics, hash: filename);
                CreateConfigFile(countReplics: replics, hash: filename,
                    filename: config_file2, distrport: storageServer2);

                CreateConfigFile(countReplics: replics, hash: filename,
                    filename: config_file3, distrport: storageServer3);

                InitInjection.RestoreUsePackage = packageRestore;

                _distrTest.Build();

                InitInjection.RestoreHelpFileOut = file1;
                _writer1.Build(storageServer1, "w1");

                InitInjection.RestoreHelpFileOut = file2;
                _writer2.Build(storageServer2, "w2", config_file2);

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

                Assert.Equal(count*replics, mem.Local + mem.Remote + mem2.Local + mem2.Remote);

                CreateHashFile(filename, 3);

                InitInjection.RestoreHelpFileOut = file3;
                _writer3.Build(storageServer3, "w3", config_file3);
                _writer3.Start();

                _writer1.Distributor.UpdateModel();
                _writer2.Distributor.UpdateModel();

                var mem3 = _writer3.Db.GetDbModules.First() as TestDbInMemory;

                _writer2.Distributor.Restore(RestoreState.FullRestoreNeed, RestoreType.Broadcast);
                _writer1.Distributor.Restore(RestoreState.FullRestoreNeed, RestoreType.Broadcast);

                Thread.Sleep(TimeSpan.FromMilliseconds(2000));

                Assert.NotEqual(0, mem3.Local);
                Assert.Equal(count*replics, mem.Local + mem2.Local + mem3.Local);

                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
                _writer3.Dispose();
            }
        }
    }
}
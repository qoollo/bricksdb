using System;
using System.Linq;
using System.Threading;
using Qoollo.Impl.Common.Support;
using Qoollo.Tests.NetMock;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestWriter;
using Xunit;

namespace Qoollo.Tests
{
    public class BroadcastRestoreTest : TestBase
    {
        public BroadcastRestoreTest():base()
        {
            _proxy = TestGate();
            _proxy.Module = new TestInjectionModule();
            _proxy.Build();
            _proxy.Start();
        }

        protected override void Dispose(bool isUserCall)
        {
            _proxy.Dispose();
        }

        [Theory]
        [InlineData(50, false)]
        [InlineData(50, true)]
        public void Simple_2Servers(int count, bool packageRestore)
        {
            var filename = nameof(Simple_2Servers);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(distrthreads: 2, countReplics: 1, hash: filename, restoreStateFilename: file1,
                    usePackage: packageRestore);
                CreateConfigFile(distrthreads: 2, countReplics: 1, hash: filename,
                    filename: config_file2, distrport: storageServer2, restoreStateFilename: file2,
                    usePackage: packageRestore);

                _distrTest.Build();

                _writer1.Build(storageServer1);
                _writer2.Build(storageServer2, configFile: config_file2);

                _writer1.Start();
                _distrTest.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer12);

                for (int i = 1; i < count + 1; i++)
                {
                    if (_proxy.Int.CreateSync(i, i).IsError)
                        _proxy.Int.CreateSync(i, i);
                }

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

                Assert.Equal(false, _writer1.Restore.IsNeedRestore);
                Assert.Equal(false, _writer2.Restore.IsNeedRestore);

                Assert.Equal(RestoreState.Restored, _writer1.Restore.RestoreState);
                Assert.Equal(RestoreState.SimpleRestoreNeed, _writer2.Restore.RestoreState);

                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
            }
        }

        [Theory]
        [InlineData(50, false)]
        [InlineData(50, true)]
        public void Simple_3Servers_OneBroadcast(int count, bool packageRestore)
        {
            var filename = nameof(Simple_3Servers_OneBroadcast);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            {
                CreateHashFile(filename, 3);
                CreateConfigFile(distrthreads: 2, countReplics: 1, hash: filename, restoreStateFilename: file1,
                    usePackage: packageRestore);
                CreateConfigFile(distrthreads: 2, countReplics: 1, hash: filename,
                    filename: config_file2, distrport: storageServer2, restoreStateFilename: file2,
                    usePackage: packageRestore);

                CreateConfigFile(distrthreads: 2, countReplics: 1, hash: filename,
                    filename: config_file3, distrport: storageServer3, restoreStateFilename: file3,
                    usePackage: packageRestore);

                _distrTest.Build();

                _writer1.Build(storageServer1, filename);
                _writer2.Build(storageServer2, filename, configFile: config_file2);
                _writer3.Build(storageServer3, filename, configFile: config_file3);

                _distrTest.Start();
                _writer1.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer12);

                for (int i = 1; i < count + 1; i++)
                {
                    if (_proxy.Int.CreateSync(i, i).IsError)
                        if (_proxy.Int.CreateSync(i, i).IsError)
                            _proxy.Int.CreateSync(i, i);
                }

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

                Assert.Equal(false, _writer1.Restore.IsNeedRestore);
                Assert.Equal(true, _writer2.Restore.IsNeedRestore);
                Assert.Equal(true, _writer3.Restore.IsNeedRestore);

                Assert.Equal(RestoreState.Restored, _writer1.Restore.RestoreState);
                Assert.Equal(RestoreState.SimpleRestoreNeed, _writer2.Restore.RestoreState);
                Assert.Equal(RestoreState.SimpleRestoreNeed, _writer3.Restore.RestoreState);

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
                CreateConfigFile(countReplics: replics, hash: filename, restoreStateFilename: file1,
                    usePackage: packageRestore);
                CreateConfigFile(distrthreads: 2, countReplics: replics, hash: filename,
                    filename: config_file2, distrport: storageServer2, restoreStateFilename: file2,
                    usePackage: packageRestore);
                CreateConfigFile(distrthreads: 2, countReplics: replics, hash: filename,
                    filename: config_file3, distrport: storageServer3, restoreStateFilename: file3,
                    usePackage: packageRestore);

                _distrTest.Build();

                _writer1.Build(storageServer1, "w1");
                _writer2.Build(storageServer2, "w2", config_file2);
                _writer3.Build(storageServer3, "w3", config_file3);

                _distrTest.Start();
                _writer1.Start();
                _writer2.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer12);

                for (int i = 1; i < count + 1; i++)
                {
                    if (_proxy.Int.CreateSync(i, i).IsError)
                        if (_proxy.Int.CreateSync(i, i).IsError)
                            _proxy.Int.CreateSync(i, i);
                }

                var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
                var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;
                var mem3 = _writer3.Db.GetDbModules.First() as TestDbInMemory;

                Assert.Equal(count * replics, mem.Local + mem.Remote + mem2.Local + mem2.Remote);

                _writer3.Start();
                Thread.Sleep(TimeSpan.FromMilliseconds(500));

                _writer2.Distributor.Restore(RestoreState.SimpleRestoreNeed, RestoreType.Broadcast);
                _writer1.Distributor.Restore(RestoreState.SimpleRestoreNeed, RestoreType.Broadcast);

                Thread.Sleep(TimeSpan.FromMilliseconds(4000));

                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);
                Assert.Equal(count * replics, mem.Local + mem2.Local + mem3.Local);

                Assert.Equal(false, _writer1.Restore.IsNeedRestore);
                Assert.Equal(false, _writer2.Restore.IsNeedRestore);
                Assert.Equal(false, _writer3.Restore.IsNeedRestore);

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
                CreateConfigFile(distrthreads: 2, countReplics: 1, hash: filename, restoreStateFilename: file1,
                    usePackage: packageRestore);

                CreateConfigFile(distrthreads: 2, countReplics: 1, hash: filename,
                    filename: config_file2, distrport: storageServer2, restoreStateFilename: file2,
                    usePackage: packageRestore);

                _distrTest.Build();

                _writer1.Build(storageServer1);

                _distrTest.Start();
                _writer1.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer12);

                for (int i = 1; i < count + 1; i++)
                {
                    if (_proxy.Int.CreateSync(i, i).IsError)
                        _proxy.Int.CreateSync(i, i);
                }

                var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
                Assert.Equal(count, mem.Local + mem.Remote);

                CreateHashFile(filename, 2);

                _writer2.Build(storageServer2, configFile: config_file2);
                _writer2.Start();

                _distrTest.Distributor.UpdateModel();

                var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;

                _writer1.Distributor.Restore(RestoreState.FullRestoreNeed, RestoreType.Broadcast);

                Thread.Sleep(TimeSpan.FromMilliseconds(2000));

                Assert.NotEqual(0, mem2.Local);
                Assert.Equal(count, mem.Local + mem2.Local);

                Assert.Equal(true, _writer1.Restore.IsNeedRestore);
                Assert.Equal(true, _writer2.Restore.IsNeedRestore);

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
                CreateConfigFile(countReplics: replics, hash: filename, restoreStateFilename: file1,
                    usePackage: packageRestore);
                CreateConfigFile(countReplics: replics, hash: filename,
                    filename: config_file2, distrport: storageServer2, restoreStateFilename: file2,
                    usePackage: packageRestore);

                CreateConfigFile(countReplics: replics, hash: filename,
                    filename: config_file3, distrport: storageServer3, restoreStateFilename: file3,
                    usePackage: packageRestore);

                _distrTest.Build();

                _writer1.Build(storageServer1, "w1");
                _writer2.Build(storageServer2, "w2", config_file2);

                _distrTest.Start();
                _writer1.Start();
                _writer2.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer12);

                for (int i = 1; i < count + 1; i++)
                {
                    if (_proxy.Int.CreateSync(i, i).IsError)
                        if (_proxy.Int.CreateSync(i, i).IsError)
                            _proxy.Int.CreateSync(i, i);
                }

                var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
                var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;

                Assert.Equal(count * replics, mem.Local + mem.Remote + mem2.Local + mem2.Remote);

                CreateHashFile(filename, 3);

                _writer3.Build(storageServer3, "w3", config_file3);
                _writer3.Start();

                _distrTest.Distributor.UpdateModel();

                var mem3 = _writer3.Db.GetDbModules.First() as TestDbInMemory;

                _writer2.Distributor.Restore(RestoreState.FullRestoreNeed, RestoreType.Broadcast);
                _writer1.Distributor.Restore(RestoreState.FullRestoreNeed, RestoreType.Broadcast);

                Thread.Sleep(TimeSpan.FromMilliseconds(2000));

                Assert.NotEqual(0, mem3.Local);
                Assert.Equal(count * replics, mem.Local + mem2.Local + mem3.Local);

                Assert.Equal(true, _writer1.Restore.IsNeedRestore);
                Assert.Equal(true, _writer2.Restore.IsNeedRestore);
                Assert.Equal(true, _writer3.Restore.IsNeedRestore);

                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
                _writer3.Dispose();
            }
        }
    }
}
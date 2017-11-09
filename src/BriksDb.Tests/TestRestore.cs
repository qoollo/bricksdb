using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Qoollo.Client.Request;
using Qoollo.Client.Support;
using Qoollo.Impl.Common.Support;
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
        public TestRestore():base()
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
        [InlineData(50)]
        public void TwoServers_SimpleRestore(int count)
        {
            var filename = nameof(TwoServers_SimpleRestore);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename, check: 100000, restoreStateFilename: file1);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename,
                    filename: config_file2, distrport: storageServer2, restoreStateFilename: file2);

                _distrTest.Build();
                _writer1.Build(storageServer1);
                _writer2.Build(storageServer2, configFile:config_file2);

                _distrTest.Start();
                _writer1.Start();

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
                _writer2.Distributor.Restore(RestoreState.SimpleRestoreNeed);

                Thread.Sleep(TimeSpan.FromMilliseconds(2000));

                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);
                Assert.Equal(count, mem.Local + mem2.Local);
                Assert.Equal(false, _writer1.Restore.IsNeedRestore);
                Assert.Equal(false, _writer2.Restore.IsNeedRestore);

                Assert.Equal(RestoreState.Restored, _writer1.Restore.RestoreState);
                Assert.Equal(RestoreState.Restored, _writer2.Restore.RestoreState);

                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
            }            
        }

        [Theory]
        [InlineData(50)]
        public void ThreeServers_SimpleRestore(int count)
        {
            var filename = nameof(ThreeServers_SimpleRestore);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            using (new FileCleaner(file4))
            {
                CreateHashFile(filename, 3);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename, check: 100000, restoreStateFilename: file1);

                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename,
                    filename: config_file2, distrport: storageServer2, restoreStateFilename: file2);

                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename,
                    filename: config_file3, distrport: storageServer3, restoreStateFilename: file3);
                
                #region hell2

                _distrTest.Build();
                _writer1.Build(storageServer1);
                _writer2.Build(storageServer2, configFile: config_file2);
                _writer3.Build(storageServer3, configFile: config_file3);

                _distrTest.Start();
                _writer1.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer12);

                for (int i = 1; i < count+1; i++)
                {
                    if (_proxy.Int.CreateSync(i, i).IsError && _proxy.Int.CreateSync(i, i).IsError)
                        _proxy.Int.CreateSync(i, i);
                }


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

                Thread.Sleep(TimeSpan.FromMilliseconds(3000));

                _writer3.Distributor.Restore(RestoreState.SimpleRestoreNeed);

                Thread.Sleep(TimeSpan.FromMilliseconds(3000));

                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);
                Assert.Equal(0, mem3.Remote);
                Assert.Equal(count, mem.Local + mem2.Local + mem3.Local);
                Assert.Equal(false, _writer1.Restore.IsNeedRestore);
                Assert.Equal(false, _writer2.Restore.IsNeedRestore);
                Assert.Equal(false, _writer3.Restore.IsNeedRestore);

                Assert.Equal(RestoreState.Restored, _writer1.Restore.RestoreState);
                Assert.Equal(RestoreState.Restored, _writer2.Restore.RestoreState);
                Assert.Equal(RestoreState.Restored, _writer3.Restore.RestoreState);

                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
                _writer3.Dispose();
            }
        }

        [Theory]
        [InlineData(50)]
        public void RestoreAfterUpdateHashFile_ThreeServers(int count)
        {
            var filename = nameof(RestoreAfterUpdateHashFile_ThreeServers);
            var filename1 = "1" + nameof(RestoreAfterUpdateHashFile_ThreeServers);
            var filename2 = "2" + nameof(RestoreAfterUpdateHashFile_ThreeServers);
            var filename3 = "3" + nameof(RestoreAfterUpdateHashFile_ThreeServers);
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
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename, filename: config_file, restoreStateFilename: file1);

                CreateHashFile(filename1, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename1, filename: config_file1, restoreStateFilename: file2);

                CreateHashFile(filename2, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename2, filename: config_file2,
                    distrport: storageServer2, restoreStateFilename: file3);

                CreateHashFile(filename3, 3);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename3, filename: config_file3,
                    distrport: storageServer3, restoreStateFilename: file4);

                _distrTest.Build(configFile: config_file);
                _writer1.Build(storageServer1, configFile: config_file1);
                _writer2.Build(storageServer2, configFile: config_file2);
                _writer3.Build(storageServer3, configFile: config_file3);

                _distrTest.Start();
                _writer1.Start();
                _writer2.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer12);

                for (int i = 1; i < count + 1; i++)
                {
                    _proxy.Int.CreateSync(i, i);
                }

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

                _writer3.Distributor.Restore(RestoreState.FullRestoreNeed);

                Thread.Sleep(TimeSpan.FromMilliseconds(3000));

                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);
                Assert.Equal(0, mem3.Remote);
                Assert.NotEqual(0, mem3.Local);
                Assert.Equal(count, mem.Local + mem2.Local + mem3.Local);
                Assert.Equal(true, _writer1.Restore.IsNeedRestore);
                Assert.Equal(true, _writer2.Restore.IsNeedRestore);
                Assert.Equal(false, _writer3.Restore.IsNeedRestore);

                Assert.Equal(RestoreState.FullRestoreNeed, _writer1.Restore.RestoreState);
                Assert.Equal(RestoreState.FullRestoreNeed, _writer2.Restore.RestoreState);
                Assert.Equal(RestoreState.Restored, _writer3.Restore.RestoreState);

                _writer1.Dispose();
                _writer2.Dispose();
                _writer3.Dispose();

                _distrTest.Dispose();
            }
        }

        [Theory]
        [InlineData(50)]
        public void SelfRestore(int count)
        {
            var filename = nameof(SelfRestore);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename);

                var factory = new TestInMemoryDbFactory(_kernel);
                var storage1 = WriterApi();
                var distr = DistributorApi();

                distr.Module = new TestInjectionModule();
                distr.Build();
                distr.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer12);

                storage1.Module = new TestInjectionModule();
                storage1.Build();
                storage1.AddDbModule(factory);
                storage1.Start();

                for (int i = 0; i < count; i++)
                {
                    if(_proxy.Int.CreateSync(i, i).IsError)
                        _proxy.Int.CreateSync(i, i);
                }

                Assert.Equal(count, factory.Db.Local + factory.Db.Remote);

                CreateHashFile(filename, 1);

                storage1.Api.UpdateModel();
                storage1.Api.Restore(RestoreMode.FullRestoreNeed);

                Thread.Sleep(1000);

                Assert.Equal(count, factory.Db.Local);

                distr.Dispose();
                storage1.Dispose();
            }
        }

        [Theory]
        [InlineData(50)]
        public void TimeoutDelete(int count)
        {
            var filename = nameof(TimeoutDelete);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                CreateHashFile(filename, 1);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename, isForceStart: true,
                    deleteTimeoutMls: 1, periodRetryMls: 60);

                var factory = new TestInMemoryDbFactory(_kernel);
                var storage1 = WriterApi();

                var distr = DistributorApi();
                distr.Module = new TestInjectionModule();
                distr.Build();
                distr.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer12);

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

                distr.Dispose();
                storage1.Dispose();
            }
        }

        [Theory]
        [InlineData(50)]
        public void ThreeServersTwoReplics(int count)
        {
            var filename = nameof(ThreeServersTwoReplics);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            using (new FileCleaner(file4))
            {
                CreateHashFile(filename, 3);
                CreateConfigFile(countReplics: 2, hash: filename, check: 100000, restoreStateFilename: file1);
                CreateConfigFile(countReplics: 2, hash: filename, filename: config_file2,
                    distrport: storageServer2, restoreStateFilename: file2);
                CreateConfigFile(countReplics: 2, hash: filename, filename: config_file3,
                    distrport: storageServer3, restoreStateFilename: file3);

                _distrTest.Build();
                _writer1.Build(storageServer1);
                _writer2.Build(storageServer2, configFile: config_file2);
                _writer3.Build(storageServer3, configFile: config_file3);

                _distrTest.Start();
                _writer1.Start();
                _writer2.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer12);

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

                Assert.Equal(RestoreState.Restored, _writer1.Restore.RestoreState);
                Assert.Equal(RestoreState.Restored, _writer2.Restore.RestoreState);
                Assert.Equal(RestoreState.Restored, _writer3.Restore.RestoreState);

                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
                _writer3.Dispose();
            }
        }

        [Theory]
        [InlineData(50)]
        public void ThreeServersTwoReplics_UpdateModel(int count)
        {
            var filename = nameof(ThreeServersTwoReplics_UpdateModel);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            using (new FileCleaner(file4))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(countReplics: 2, hash: filename, restoreStateFilename: file1);
                CreateConfigFile(countReplics: 2, hash: filename, filename: config_file2,
                    distrport: storageServer2, restoreStateFilename: file2);
                CreateConfigFile(countReplics: 2, hash: filename, filename: config_file3,
                    distrport: storageServer3, restoreStateFilename: file3);

                _distrTest.Build();
                _writer1.Build(storageServer1);
                _writer2.Build(storageServer2, configFile: config_file2);
                _writer3.Build(storageServer3, configFile: config_file3);

                _distrTest.Start();
                _writer1.Start();
                _writer2.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer12);

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

                _distrTest.Distributor.UpdateModel();

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

                Assert.Equal(RestoreState.Restored, _writer1.Restore.RestoreState);
                Assert.Equal(RestoreState.Restored, _writer2.Restore.RestoreState);
                Assert.Equal(RestoreState.Restored, _writer3.Restore.RestoreState);

                Assert.NotEqual(localLast, mem.Local);
                Assert.NotEqual(localLast2, mem2.Local);

                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
                _writer3.Dispose();
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
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename, check: 100, restoreStateFilename: file1);
                CreateConfigFile(countReplics: 1, hash: filename, filename: config_file2,
                    distrport: storageServer2, restoreStateFilename: file2);

                _writer1.Build(storageServer1);
                _writer2.Build(storageServer2, configFile: config_file2);

                _writer1.Start();

                _distrTest.Build();
                _distrTest.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer12);

                var mem = _writer1.Db.GetDbModules.First() as TestDbInMemory;
                var mem2 = _writer2.Db.GetDbModules.First() as TestDbInMemory;

                for (int i = 0; i < count; i++)
                {
                    if (_proxy.Int.CreateSync(i, i).IsError)
                        _proxy.Int.CreateSync(i, i);
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
                    if (_proxy.Int.CreateSync(i + 50, i).IsError)
                        _proxy.Int.CreateSync(i + 50, i);
                }

                Assert.Equal(count * 2, mem.Local + mem2.Local);

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
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename, filename: config_file, check: 1000, restoreStateFilename: file1);

                CreateHashFile(filename2, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename2, filename: config_file2, restoreStateFilename: file2);

                CreateHashFile(filename3, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename3, filename: config_file3,
                    distrport: storageServer2, restoreStateFilename: file3);

                CreateHashFile(filename4, 3);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename4, filename: config_file4,
                    distrport: storageServer3, restoreStateFilename: file4);

                _writer1.Build(storageServer1, configFile: config_file2);
                _writer2.Build(storageServer2, configFile: config_file3);

                _writer1.Start();
                _writer2.Start();

                _distrTest.Build(configFile: config_file);
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

                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
                _writer3.Dispose();
            }
        }

        [Theory]
        [InlineData(50)]
        public void TwoServer_RestoreFromDistributor(int count)
        {
            var filename = nameof(TwoServer_RestoreFromDistributor);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename, check: 100, restoreStateFilename: file1,
                    autoRestoreEnable: true);
                CreateConfigFile(countReplics: 1, hash: filename, filename: config_file2,
                    distrport: storageServer2, restoreStateFilename: file2);

                _distrTest.Build();

                _writer1.Build(storageServer1);
                _writer2.Build(storageServer2, configFile: config_file2);

                _distrTest.Start();
                _writer1.Start();

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
                Assert.Equal(0, mem2.Local + mem2.Remote);

                _writer2.Start();

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);
                Assert.Equal(count, mem.Local + mem2.Local);
                Assert.Equal(false, _writer1.Restore.IsNeedRestore);
                Assert.Equal(false, _writer2.Restore.IsNeedRestore);

                Assert.Equal(RestoreState.Restored, _writer1.Restore.RestoreState);
                Assert.Equal(RestoreState.Restored, _writer2.Restore.RestoreState);
                
                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
            }
        }

        [Theory]
        [InlineData(50)]
        public void TwoServer_RestoreFromDistributor_EnableCommand(int count)
        {
            var filename = nameof(TwoServer_RestoreFromDistributor_EnableCommand);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            using (new FileCleaner(file4))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename, check: 100, restoreStateFilename: file1,
                    autoRestoreEnable: true);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename,
                    filename: config_file2, distrport: storageServer2, restoreStateFilename: file2);

                _distrTest.Build();

                _writer1.Build(storageServer1);
                _writer2.Build(storageServer2, configFile: config_file2);

                _distrTest.Start();
                _distrTest.Distributor.AutoRestoreSetMode(false);

                _writer1.Start();

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

                Assert.Equal(RestoreState.Restored, _writer1.Restore.RestoreState);
                Assert.Equal(RestoreState.Restored, _writer2.Restore.RestoreState);
                
                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
            }
        }

        [Theory]
        [InlineData(50)]
        public void TwoServer_RestoreFromDistributorWithCommand(int count)
        {
            var filename = nameof(TwoServer_RestoreFromDistributorWithCommand);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            using (new FileCleaner(file4))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename, check: 100, restoreStateFilename: file1);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename,
                    filename: config_file2, distrport: storageServer2, restoreStateFilename: file2);

                _distrTest.Build();

                _writer1.Build(storageServer1);
                _writer2.Build(storageServer2, configFile: config_file2);

                _distrTest.Start();
                _writer1.Start();

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
                Assert.Equal(0, mem2.Local + mem2.Remote);

                _writer2.Start();
                _distrTest.Distributor.Restore(ServerId(storageServer2), ServerId(storageServer1),
                    RestoreState.SimpleRestoreNeed);

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);
                Assert.Equal(count, mem.Local + mem2.Local);
                Assert.Equal(false, _writer1.Restore.IsNeedRestore);
                Assert.Equal(false, _writer2.Restore.IsNeedRestore);

                Assert.Equal(RestoreState.Restored, _writer1.Restore.RestoreState);
                Assert.Equal(RestoreState.Restored, _writer2.Restore.RestoreState);
                
                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
            }
        }

        [Theory]
        [InlineData(50)]
        public void TwoServers_Package(int count)
        {
            var filename = nameof(TwoServers_Package);
            using (new FileCleaner(filename))
            using (new FileCleaner(file1))
            using (new FileCleaner(file2))
            using (new FileCleaner(file3))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename, check: 100000, restoreStateFilename: file1, usePackage: true);
                CreateConfigFile(distrthreads: 1, countReplics: 1, hash: filename,
                    filename: config_file2, distrport: storageServer2, restoreStateFilename: file2, usePackage: true);

                _distrTest.Build();
                _writer1.Build(storageServer1);
                _writer2.Build(storageServer2, configFile: config_file2);

                _distrTest.Start();
                _writer1.Start();

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
                _writer2.Distributor.Restore(RestoreState.SimpleRestoreNeed);

                Thread.Sleep(TimeSpan.FromMilliseconds(2000));

                Assert.Equal(0, mem.Remote);
                Assert.Equal(0, mem2.Remote);
                Assert.Equal(count, mem.Local + mem2.Local);
                Assert.Equal(false, _writer1.Restore.IsNeedRestore);
                Assert.Equal(false, _writer2.Restore.IsNeedRestore);

                Assert.Equal(RestoreState.Restored, _writer1.Restore.RestoreState);
                Assert.Equal(RestoreState.Restored, _writer2.Restore.RestoreState);
                
                _distrTest.Dispose();
                _writer1.Dispose();
                _writer2.Dispose();
            }
        }
    }
}

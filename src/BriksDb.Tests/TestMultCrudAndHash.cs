﻿using System;
using System.Threading;
using Ninject;
using Qoollo.Client.Configuration;
using Qoollo.Client.DistributorGate;
using Qoollo.Client.Request;
using Qoollo.Client.Support;
using Qoollo.Client.WriterGate;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.TestSupport;
using Qoollo.Tests.NetMock;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestModules;
using Qoollo.Tests.TestWriter;
using Xunit;
using Consts = Qoollo.Impl.Common.Support.Consts;

namespace Qoollo.Tests
{
    [Collection("test collection 1")]
    public class TestMultCrudAndHash:TestBase
    {
        private TestGate _proxyGate;
        new const int proxyServer = 22378;
        
        public TestMultCrudAndHash():base()
        {
            InitInjection.Kernel = new StandardKernel(new TestInjectionModule());

            var common = new CommonConfiguration(1, 100);

            var netconfig = new NetConfiguration("localhost", proxyServer, "testService", 10);
            var toconfig = new ProxyConfiguration(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));

            _proxyGate = new TestGate(netconfig, toconfig, common);
            _proxyGate.Build();
        }

        [Fact]
        public void Proxy_CRUD_TwoTables()
        {            
            const int distrServer1 = 22379;
            const int distrServer12 = 22380;
            const int st1 = 22381;
            const int st2 = 22382;

            var filename = nameof(Proxy_CRUD_TwoTables);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))

            {
                #region hell

                var writer = new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 1, 1, HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", st1, st2);
                writer.Save();

                var common = new CommonConfiguration(1, 100);

                var distrNet = new DistributorNetConfiguration("localhost",
                    distrServer1, distrServer12, "testService", 10);
                var distrConf = new DistributorConfiguration(1, filename,
                    TimeSpan.FromMilliseconds(100000), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(10000));

                var distr = new DistributorApi(distrNet, distrConf, common);

                var storageNet = new StorageNetConfiguration("localhost", st1, st2, "testService", 10);
                var storageConfig = new StorageConfiguration(filename, 1, 10, TimeSpan.FromHours(1), TimeSpan.FromHours(1),
                    TimeSpan.FromHours(1), TimeSpan.FromHours(1), false);

                var storage = new WriterApi(storageNet, storageConfig, common);

                #endregion

                storage.Build();
                distr.Build();

                var f = new TestInMemoryDbFactory();
                var f2 = new TestInMemoryDbFactory("Int2");

                storage.AddDbModule(f);
                storage.AddDbModule(f2);

                storage.Start();
                _proxyGate.Start();
                distr.Start();

                _proxyGate.Int.SayIAmHere("localhost", distrServer1);

                const int count = 5;

                for (int i = 0; i < count; i++)
                {
                    RequestDescription result;

                    _proxyGate.Int.Read(i, out result);
                    Assert.Equal(RequestState.DataNotFound, result.State);
                    _proxyGate.Int2.Read(i, out result);
                    Assert.Equal(RequestState.DataNotFound, result.State);
                }

                for (int i = 0; i < count; i++)
                {
                    RequestDescription result = _proxyGate.Int.CreateSync(i, i);
                    Assert.Equal(RequestState.Complete, result.State);
                    result = _proxyGate.Int2.CreateSync(i, i);
                    Assert.Equal(RequestState.Complete, result.State);
                }

                for (int i = 0; i < count; i++)
                {
                    RequestDescription result;

                    var value = _proxyGate.Int.Read(i, out result);
                    Assert.Equal(RequestState.Complete, result.State);
                    Assert.Equal(i, value);
                    value = _proxyGate.Int2.Read(i, out result);
                    Assert.Equal(i, value);
                }

                Assert.Equal(count, f.Db.Local);
                Assert.Equal(count, f2.Db.Local);

                _proxyGate.Dispose();
                distr.Dispose();
                storage.Dispose();
            }
        }

        [Fact]
        public void Proxy_Restore_TwoTablesOneCommand()
        {            
            const int distrServer1 = 22384;
            const int distrServer12 = 22385;
            const int st1 = 22386;
            const int st2 = 22387;
            const int st3 = 22388;
            const int st4 = 22389;

            var filename = nameof(Proxy_Restore_TwoTablesOneCommand);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                #region hell

                var writer = new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 2, 1, HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", st1, st2);
                writer.SetServer(1, "localhost", st3, st4);
                writer.Save();

                var common = new CommonConfiguration(1, 100);

                var distrNet = new DistributorNetConfiguration("localhost",
                    distrServer1, distrServer12, "testService", 10);
                var distrConf = new DistributorConfiguration(1, filename,
                    TimeSpan.FromMilliseconds(100000), TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(10000));

                var distr = new DistributorApi(distrNet, distrConf, common);

                var storageNet1 = new StorageNetConfiguration("localhost", st1, st2, "testService", 10);
                var storageNet2 = new StorageNetConfiguration("localhost", st3, st4, "testService", 10);
                var storageConfig = new StorageConfiguration(filename, 1, 10, TimeSpan.FromHours(1),
                    TimeSpan.FromHours(1), TimeSpan.FromHours(1), TimeSpan.FromHours(1), false);

                var storage1 = new WriterApi(storageNet1, storageConfig, common);
                var storage2 = new WriterApi(storageNet2, storageConfig, common);

                #endregion

                storage1.Build();
                storage2.Build();
                distr.Build();

                var f = new TestInMemoryDbFactory();
                var f2 = new TestInMemoryDbFactory("Int2");
                storage1.AddDbModule(f);
                storage1.AddDbModule(f2);

                var f3 = new TestInMemoryDbFactory();
                var f4 = new TestInMemoryDbFactory("Int2");
                storage2.AddDbModule(f3);
                storage2.AddDbModule(f4);

                storage1.Start();
                _proxyGate.Start();
                distr.Start();

                _proxyGate.Int.SayIAmHere("localhost", distrServer1);

                const int count = 5;

                for (int i = 0; i < count; i++)
                {
                    _proxyGate.Int.CreateSync(i, i);
                    _proxyGate.Int2.CreateSync(i, i);

                    _proxyGate.Int.CreateSync(i, i);
                    _proxyGate.Int2.CreateSync(i, i);
                }

                for (int i = 0; i < count; i++)
                {
                    RequestDescription result;

                    var value = _proxyGate.Int.Read(i, out result);
                    Assert.Equal(RequestState.Complete, result.State);
                    Assert.Equal(i, value);
                    value = _proxyGate.Int2.Read(i, out result);
                    Assert.Equal(i, value);
                }

                Assert.Equal(count, f.Db.Local + f.Db.Remote);
                Assert.Equal(count, f2.Db.Local + f2.Db.Remote);

                storage2.Start();
                storage2.Api.Restore(RestoreMode.SimpleRestoreNeed);

                Thread.Sleep(TimeSpan.FromMilliseconds(2000));

                Assert.Equal(count, f.Db.Local + f3.Db.Local);
                Assert.Equal(count, f2.Db.Local + f4.Db.Local);

                _proxyGate.Dispose();
                distr.Dispose();
                storage1.Dispose();
                storage2.Dispose();
            }
        }

        [Fact]
        public void Proxy_Restore_TwoTablesTwoCommands()
        {            
            const int distrServer1 = 22391;
            const int distrServer12 = 22392;
            const int st1 = 22393;
            const int st2 = 22394;
            const int st3 = 22395;
            const int st4 = 22396;

            var filename = nameof(Proxy_Restore_TwoTablesTwoCommands);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                #region hell

                var writer = new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 2, 1, HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", st1, st2);
                writer.SetServer(1, "localhost", st3, st4);
                writer.Save();

                var common = new CommonConfiguration(1, 100);

                var distrNet = new DistributorNetConfiguration("localhost",
                    distrServer1, distrServer12, "testService", 10);
                var distrConf = new DistributorConfiguration(1, filename,
                    TimeSpan.FromMilliseconds(1000000), TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(1000000));

                var distr = new DistributorApi(distrNet, distrConf, common);

                var storageNet1 = new StorageNetConfiguration("localhost", st1, st2, "testService", 10);
                var storageNet2 = new StorageNetConfiguration("localhost", st3, st4, "testService", 10);
                var storageConfig = new StorageConfiguration(filename, 1, 10, TimeSpan.FromHours(1),
                    TimeSpan.FromSeconds(1), TimeSpan.FromHours(1), TimeSpan.FromHours(1), false);

                var storage1 = new WriterApi(storageNet1, storageConfig, common);
                var storage2 = new WriterApi(storageNet2, storageConfig, common);

                #endregion

                storage1.Build();
                storage2.Build();
                distr.Build();

                var f = new TestInMemoryDbFactory();
                var f2 = new TestInMemoryDbFactory("Int2");
                storage1.AddDbModule(f);
                storage1.AddDbModule(f2);

                var f3 = new TestInMemoryDbFactory();
                var f4 = new TestInMemoryDbFactory("Int2");
                storage2.AddDbModule(f3);
                storage2.AddDbModule(f4);

                storage1.Start();
                _proxyGate.Start();
                distr.Start();

                _proxyGate.Int.SayIAmHere("localhost", distrServer1);

                const int count = 5;

                for (int i = 0; i < count; i++)
                {
                    _proxyGate.Int.CreateSync(i, i);
                    _proxyGate.Int2.CreateSync(i, i);

                    _proxyGate.Int.CreateSync(i, i);
                    _proxyGate.Int2.CreateSync(i, i);
                }

                for (int i = 0; i < count; i++)
                {
                    RequestDescription result;

                    var value = _proxyGate.Int.Read(i, out result);
                    Assert.Equal(RequestState.Complete, result.State);
                    Assert.Equal(i, value);
                    value = _proxyGate.Int2.Read(i, out result);
                    Assert.Equal(i, value);
                }

                Assert.Equal(count, f.Db.Local + f.Db.Remote);
                Assert.Equal(count, f2.Db.Local + f2.Db.Remote);

                storage2.Start();
                storage2.Api.Restore(RestoreMode.SimpleRestoreNeed, "Int");
                Thread.Sleep(TimeSpan.FromMilliseconds(2000));

                Assert.Equal(count, f.Db.Local + f3.Db.Local);
                Assert.Equal(count, f2.Db.Local + f2.Db.Remote);

                storage2.Api.Restore(RestoreMode.SimpleRestoreNeed, "Int2");
                Thread.Sleep(TimeSpan.FromMilliseconds(2000));

                Assert.Equal(count, f.Db.Local + f3.Db.Local);
                Assert.Equal(count, f2.Db.Local + f4.Db.Local);

                _proxyGate.Dispose();
                distr.Dispose();
                storage1.Dispose();
                storage2.Dispose();
            }
        }

        [Fact]
        public void Proxy_HashFromValue()
        {
            const int distrServer1 = 22398;
            const int distrServer12 = 22399;
            const int st1 = 22400;
            const int st2 = 22401;

            var filename = nameof(Proxy_HashFromValue);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                #region hell

                var writer =
                new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 1, 1,
                    HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", st1, st2);
                writer.Save();

                var common = new CommonConfiguration(1, 100);

                var distrNet = new DistributorNetConfiguration("localhost",
                    distrServer1, distrServer12, "testService", 10);
                var distrConf = new DistributorConfiguration(1, filename,
                    TimeSpan.FromMilliseconds(100000), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1),
                    TimeSpan.FromMilliseconds(10000));

                var distr = new DistributorApi(distrNet, distrConf, common);

                var storageNet = new StorageNetConfiguration("localhost", st1, st2, "testService", 10);
                var storageConfig = new StorageConfiguration(filename, 1, 10, TimeSpan.FromHours(1), TimeSpan.FromHours(1),
                    TimeSpan.FromHours(1), TimeSpan.FromHours(1), false);

                var storage = new WriterApi(storageNet, storageConfig, common);

                #endregion

                storage.Build();
                distr.Build();

                var f = new TestInMemoryDbFactory("Int3", new IntHashConvertor());

                storage.AddDbModule(f);

                storage.Start();
                _proxyGate.Start();
                distr.Start();

                _proxyGate.Int.SayIAmHere("localhost", distrServer1);

                const int count = 5;

                for (int i = 0; i < count; i++)
                {
                    RequestDescription result;

                    _proxyGate.Int3.Read(i, out result);
                    Assert.Equal(RequestState.DataNotFound, result.State);
                }

                for (int i = 0; i < count; i++)
                {
                    RequestDescription result = _proxyGate.Int3.CreateSync(i, i);
                    Assert.Equal(RequestState.Complete, result.State);
                }

                for (int i = 0; i < count; i++)
                {
                    RequestDescription result;

                    var value = _proxyGate.Int3.Read(i, out result);
                    Assert.Equal(RequestState.Complete, result.State);
                    Assert.Equal(i, value);
                }

                Assert.Equal(count, f.Db.Local);

                _proxyGate.Dispose();
                distr.Dispose();
                storage.Dispose();
            }
        }

        [Fact]
        public void Proxy_Restore_HashFromValue()
        {
            const int distrServer1 = 22403;
            const int distrServer12 = 22404;
            const int st1 = 22405;
            const int st2 = 22406;
            const int st3 = 22407;
            const int st4 = 22408;

            var filename = nameof(Proxy_Restore_HashFromValue);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                #region hell

                var writer =
                new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 2, 2,
                    HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", st1, st2);
                writer.SetServer(1, "localhost", st3, st4);
                writer.Save();

                var common = new CommonConfiguration(1, 100);

                var distrNet = new DistributorNetConfiguration("localhost",
                    distrServer1, distrServer12, "testService", 10);
                var distrConf = new DistributorConfiguration(1, filename,
                    TimeSpan.FromMilliseconds(100000), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1),
                    TimeSpan.FromMilliseconds(10000));

                var distr = new DistributorApi(distrNet, distrConf, common);

                var storageNet1 = new StorageNetConfiguration("localhost", st1, st2, "testService", 10);
                var storageNet2 = new StorageNetConfiguration("localhost", st3, st4, "testService", 10);
                var storageConfig = new StorageConfiguration(filename, 1, 10, TimeSpan.FromHours(1),
                    TimeSpan.FromHours(1), TimeSpan.FromHours(1), TimeSpan.FromHours(1), false);

                var storage1 = new WriterApi(storageNet1, storageConfig, common);
                var storage2 = new WriterApi(storageNet2, storageConfig, common);

                #endregion

                storage1.Build();
                storage2.Build();
                distr.Build();

                var f1 = new TestInMemoryDbFactory("Int3", new IntHashConvertor());
                var f2 = new TestInMemoryDbFactory("Int3", new IntHashConvertor());

                storage1.AddDbModule(f1);
                storage2.AddDbModule(f2);

                storage1.Start();
                _proxyGate.Start();
                distr.Start();

                _proxyGate.Int.SayIAmHere("localhost", distrServer1);

                const int count = 50;

                for (int i = 0; i < count; i++)
                {
                    _proxyGate.Int3.CreateSync(i, i);
                    _proxyGate.Int3.CreateSync(i, i);
                }

                for (int i = 0; i < count; i++)
                {
                    RequestDescription result;

                    var value = _proxyGate.Int3.Read(i, out result);
                    Assert.Equal(RequestState.Complete, result.State);
                    Assert.Equal(i, value);
                }

                Assert.Equal(count, f1.Db.Local + f1.Db.Remote);

                storage2.Start();

                storage2.Api.Restore(RestoreMode.SimpleRestoreNeed);
                Thread.Sleep(TimeSpan.FromMilliseconds(2000));

                Assert.Equal(count, f1.Db.Local + f2.Db.Local);
                Assert.Equal(0, f1.Db.Remote);
                Assert.Equal(0, f2.Db.Remote);

                _proxyGate.Dispose();
                distr.Dispose();
                storage1.Dispose();
                storage2.Dispose();
            }
        }
    }
}

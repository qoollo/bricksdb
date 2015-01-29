using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Client.Configuration;
using Qoollo.Client.DistributorGate;
using Qoollo.Client.Request;
using Qoollo.Client.WriterGate;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Configurations;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestModules;
using Qoollo.Tests.TestWriter;

namespace Qoollo.Tests
{
    [TestClass]
    public class TestMultCrudAndHash
    {
        private TestGate _proxy;
        const int proxyServer = 22378;
        [TestInitialize]
        public void Initialize()
        {
            var common = new CommonConfiguration(1, 100);

            var netconfig = new NetConfiguration("localhost", proxyServer, "testService", 10);
            var toconfig = new ProxyConfiguration(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));

            _proxy = new TestGate(netconfig, toconfig, common);
            _proxy.Build();
        }

        [TestMethod]
        public void Proxy_CRUD_TwoTables()
        {            
            const int distrServer1 = 22379;
            const int distrServer12 = 22380;
            const int st1 = 22381;
            const int st2 = 22382;

            #region hell

            var writer = new HashWriter(new HashMapConfiguration("Test2Crud", HashMapCreationMode.CreateNew, 1, 1, HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", st1, st2);
            writer.Save();

            var common = new CommonConfiguration(1, 100);

            var distrNet = new DistributorNetConfiguration("localhost",
                distrServer1, distrServer12, "testService", 10);
            var distrConf = new DistributorConfiguration(1, "Test2Crud",
                TimeSpan.FromMilliseconds(100000), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(10000));

            var distr = new DistributorApi(distrNet, distrConf, common);

            var storageNet = new StorageNetConfiguration("localhost", st1, st2, "testService", 10);
            var storageConfig = new StorageConfiguration("Test2Crud", 1, 10, TimeSpan.FromHours(1), TimeSpan.FromHours(1),
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
            _proxy.Start();
            distr.Start();

            _proxy.Int.SayIAmHere("localhost", distrServer1);

            const int count = 5;

            for (int i = 0; i < count; i++)
            {
                RequestDescription result;

                _proxy.Int.Read(i, out result);
                Assert.AreEqual(RequestState.DataNotFound, result.State);
                _proxy.Int2.Read(i, out result);
                Assert.AreEqual(RequestState.DataNotFound, result.State);
            }

            for (int i = 0; i < count; i++)
            {
                RequestDescription result = _proxy.Int.CreateSync(i, i);
                Assert.AreEqual(RequestState.Complete, result.State);
                result = _proxy.Int2.CreateSync(i, i);
                Assert.AreEqual(RequestState.Complete, result.State);
            }

            for (int i = 0; i < count; i++)
            {
                RequestDescription result;

                var value = _proxy.Int.Read(i, out result);
                Assert.AreEqual(RequestState.Complete, result.State);
                Assert.AreEqual(i, value);
                value = _proxy.Int2.Read(i, out result);
                Assert.AreEqual(i, value);
            }

            Assert.AreEqual(count, f.Db.Local);
            Assert.AreEqual(count, f2.Db.Local);

            _proxy.Dispose();
            distr.Dispose();
            storage.Dispose();
        }

        [TestMethod]
        public void Proxy_Restore_TwoTablesOneCommand()
        {            
            const int distrServer1 = 22384;
            const int distrServer12 = 22385;
            const int st1 = 22386;
            const int st2 = 22387;
            const int st3 = 22388;
            const int st4 = 22389;

            #region hell

            var writer = new HashWriter(new HashMapConfiguration("Test2CrudRestore",
                HashMapCreationMode.CreateNew, 2, 1, HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", st1, st2);
            writer.SetServer(1, "localhost", st3, st4);
            writer.Save();

            var common = new CommonConfiguration(1, 100);

            var distrNet = new DistributorNetConfiguration("localhost",
                distrServer1, distrServer12, "testService", 10);
            var distrConf = new DistributorConfiguration(1, "Test2CrudRestore",
                TimeSpan.FromMilliseconds(100000), TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(10000));

            var distr = new DistributorApi(distrNet, distrConf, common);

            var storageNet1 = new StorageNetConfiguration("localhost", st1, st2, "testService", 10);
            var storageNet2 = new StorageNetConfiguration("localhost", st3, st4, "testService", 10);
            var storageConfig = new StorageConfiguration("Test2CrudRestore", 1, 10, TimeSpan.FromHours(1),
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
            _proxy.Start();
            distr.Start();

            _proxy.Int.SayIAmHere("localhost", distrServer1);

            const int count = 5;

            for (int i = 0; i < count; i++)
            {
                _proxy.Int.CreateSync(i, i);
                _proxy.Int2.CreateSync(i, i);

                _proxy.Int.CreateSync(i, i);
                _proxy.Int2.CreateSync(i, i);
            }

            for (int i = 0; i < count; i++)
            {
                RequestDescription result;

                var value = _proxy.Int.Read(i, out result);
                Assert.AreEqual(RequestState.Complete, result.State);
                Assert.AreEqual(i, value);
                value = _proxy.Int2.Read(i, out result);
                Assert.AreEqual(i, value);
            }

            Assert.AreEqual(count, f.Db.Local + f.Db.Remote);
            Assert.AreEqual(count, f2.Db.Local + f2.Db.Remote);

            storage2.Start();
            storage2.Api.Restore(new ServerAddress("localhost", distrServer12), false);

            Thread.Sleep(TimeSpan.FromMilliseconds(2000));

            Assert.AreEqual(count, f.Db.Local + f3.Db.Local);
            Assert.AreEqual(count, f2.Db.Local + f4.Db.Local);

            _proxy.Dispose();
            distr.Dispose();
            storage1.Dispose();
            storage2.Dispose();
        }

        [TestMethod]
        public void Proxy_Restore_TwoTablesTwoCommands()
        {            
            const int distrServer1 = 22391;
            const int distrServer12 = 22392;
            const int st1 = 22393;
            const int st2 = 22394;
            const int st3 = 22395;
            const int st4 = 22396;

            #region hell

            var writer = new HashWriter(new HashMapConfiguration("TestCrudRestoreSingle",
                HashMapCreationMode.CreateNew, 2, 1, HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", st1, st2);
            writer.SetServer(1, "localhost", st3, st4);
            writer.Save();

            var common = new CommonConfiguration(1, 100);

            var distrNet = new DistributorNetConfiguration("localhost",
                distrServer1, distrServer12, "testService", 10);
            var distrConf = new DistributorConfiguration(1, "TestCrudRestoreSingle",
                TimeSpan.FromMilliseconds(100000), TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(10000));

            var distr = new DistributorApi(distrNet, distrConf, common);

            var storageNet1 = new StorageNetConfiguration("localhost", st1, st2, "testService", 10);
            var storageNet2 = new StorageNetConfiguration("localhost", st3, st4, "testService", 10);
            var storageConfig = new StorageConfiguration("TestCrudRestoreSingle", 1, 10, TimeSpan.FromHours(1),
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
            _proxy.Start();
            distr.Start();

            _proxy.Int.SayIAmHere("localhost", distrServer1);

            const int count = 5;

            for (int i = 0; i < count; i++)
            {
                _proxy.Int.CreateSync(i, i);
                _proxy.Int2.CreateSync(i, i);

                _proxy.Int.CreateSync(i, i);
                _proxy.Int2.CreateSync(i, i);
            }

            for (int i = 0; i < count; i++)
            {
                RequestDescription result;

                var value = _proxy.Int.Read(i, out result);
                Assert.AreEqual(RequestState.Complete, result.State);
                Assert.AreEqual(i, value);
                value = _proxy.Int2.Read(i, out result);
                Assert.AreEqual(i, value);
            }

            Assert.AreEqual(count, f.Db.Local + f.Db.Remote);
            Assert.AreEqual(count, f2.Db.Local + f2.Db.Remote);

            storage2.Start();
            storage2.Api.Restore(new ServerAddress("localhost", distrServer12), false, "Int");
            Thread.Sleep(TimeSpan.FromMilliseconds(2000));

            Assert.AreEqual(count, f.Db.Local + f3.Db.Local);
            Assert.AreEqual(count, f2.Db.Local + f2.Db.Remote);

            storage2.Api.Restore(new ServerAddress("localhost", distrServer12), false, "Int2");
            Thread.Sleep(TimeSpan.FromMilliseconds(2000));

            Assert.AreEqual(count, f.Db.Local + f3.Db.Local);
            Assert.AreEqual(count, f2.Db.Local + f4.Db.Local);

            _proxy.Dispose();
            distr.Dispose();
            storage1.Dispose();
            storage2.Dispose();
        }

        [TestMethod]
        public void Proxy_HashFromValue()
        {
            const int distrServer1 = 22398;
            const int distrServer12 = 22399;
            const int st1 = 22400;
            const int st2 = 22401;

            #region hell

            var writer =
                new HashWriter(new HashMapConfiguration("TestHashFromValue", HashMapCreationMode.CreateNew, 1, 1,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", st1, st2);
            writer.Save();

            var common = new CommonConfiguration(1, 100);

            var distrNet = new DistributorNetConfiguration("localhost",
                distrServer1, distrServer12, "testService", 10);
            var distrConf = new DistributorConfiguration(1, "TestHashFromValue",
                TimeSpan.FromMilliseconds(100000), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1),
                TimeSpan.FromMilliseconds(10000));

            var distr = new DistributorApi(distrNet, distrConf, common);

            var storageNet = new StorageNetConfiguration("localhost", st1, st2, "testService", 10);
            var storageConfig = new StorageConfiguration("TestHashFromValue", 1, 10, TimeSpan.FromHours(1), TimeSpan.FromHours(1),
                TimeSpan.FromHours(1), TimeSpan.FromHours(1), false);

            var storage = new WriterApi(storageNet, storageConfig, common);

            #endregion

            storage.Build();
            distr.Build();

            var f = new TestInMemoryDbFactory("Int3", new IntHashConvertor());

            storage.AddDbModule(f);

            storage.Start();
            _proxy.Start();
            distr.Start();

            _proxy.Int.SayIAmHere("localhost", distrServer1);

            const int count = 5;

            for (int i = 0; i < count; i++)
            {
                RequestDescription result;

                _proxy.Int3.Read(i, out result);
                Assert.AreEqual(RequestState.DataNotFound, result.State);
            }

            for (int i = 0; i < count; i++)
            {
                RequestDescription result = _proxy.Int3.CreateSync(i, i);
                Assert.AreEqual(RequestState.Complete, result.State);
            }

            for (int i = 0; i < count; i++)
            {
                RequestDescription result;

                var value = _proxy.Int3.Read(i, out result);
                Assert.AreEqual(RequestState.Complete, result.State);
                Assert.AreEqual(i, value);
            }

            Assert.AreEqual(count, f.Db.Local);

            _proxy.Dispose();
            distr.Dispose();
            storage.Dispose();
        }

        [TestMethod]
        public void Proxy_Restore_HashFromValue()
        {
            const int distrServer1 = 22403;
            const int distrServer12 = 22404;
            const int st1 = 22405;
            const int st2 = 22406;
            const int st3 = 22407;
            const int st4 = 22408;

            #region hell

            var writer =
                new HashWriter(new HashMapConfiguration("TestHashFromValue", HashMapCreationMode.CreateNew, 2, 2,
                    HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", st1, st2);
            writer.SetServer(1, "localhost", st3, st4);
            writer.Save();

            var common = new CommonConfiguration(1, 100);

            var distrNet = new DistributorNetConfiguration("localhost",
                distrServer1, distrServer12, "testService", 10);
            var distrConf = new DistributorConfiguration(1, "TestHashFromValue",
                TimeSpan.FromMilliseconds(100000), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1),
                TimeSpan.FromMilliseconds(10000));

            var distr = new DistributorApi(distrNet, distrConf, common);

            var storageNet1 = new StorageNetConfiguration("localhost", st1, st2, "testService", 10);
            var storageNet2 = new StorageNetConfiguration("localhost", st3, st4, "testService", 10);
            var storageConfig = new StorageConfiguration("TestHashFromValue", 1, 10, TimeSpan.FromHours(1),
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
            _proxy.Start();
            distr.Start();

            _proxy.Int.SayIAmHere("localhost", distrServer1);

            const int count = 50;

            for (int i = 0; i < count; i++)
            {
                _proxy.Int3.CreateSync(i, i);
                _proxy.Int3.CreateSync(i, i);
            }

            for (int i = 0; i < count; i++)
            {
                RequestDescription result;

                var value = _proxy.Int3.Read(i, out result);
                Assert.AreEqual(RequestState.Complete, result.State);
                Assert.AreEqual(i, value);
            }

            Assert.AreEqual(count, f1.Db.Local + f1.Db.Remote);

            storage2.Start();

            storage2.Api.Restore(new ServerAddress("localhost", distrServer12), false);
            Thread.Sleep(TimeSpan.FromMilliseconds(2000));

            Assert.AreEqual(count, f1.Db.Local + f2.Db.Local);
            Assert.AreEqual(0, f1.Db.Remote);
            Assert.AreEqual(0, f2.Db.Remote);

            _proxy.Dispose();
            distr.Dispose();
            storage1.Dispose();
            storage2.Dispose();
        }
    }
}

﻿using System;
using System.Threading;
using Qoollo.Client.Request;
using Qoollo.Client.Support;
using Qoollo.Tests.NetMock;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestWriter;
using Xunit;
using Consts = Qoollo.Impl.Common.Support.Consts;

namespace Qoollo.Tests
{
    [Collection("test collection 1")]
    public class TestMultCrudAndHash:TestBase
    {
        public TestMultCrudAndHash():base()
        {
            _proxy = TestGate(proxyServer, 30000);

            _proxy.Module = new TestInjectionModule();
            _proxy.Build();
        }

        [Theory]
        [InlineData(50)]
        [InlineData(500)]
        public void Proxy_CRUD_TwoTables(int count)
        {            
            var filename = nameof(Proxy_CRUD_TwoTables);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                CreateHashFile(filename, 1);
                CreateConfigFile(countReplics: 1, hash: filename);

                var distr = DistributorApi(DistributorConfiguration(filename, 1), distrServer1, distrServer12);
                var storage = WriterApi(StorageConfiguration(filename, 1), storageServer1);

                storage.Module = new TestInjectionModule();
                storage.Build();

                distr.Module = new TestInjectionModule();
                distr.Build();

                var f = new TestInMemoryDbFactory(_kernel);
                var f2 = new TestInMemoryDbFactory(_kernel, "Int2");

                storage.AddDbModule(f);
                storage.AddDbModule(f2);

                storage.Start();
                _proxy.Start();
                distr.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer1);

                for (int i = 0; i < count; i++)
                {
                    RequestDescription result;

                    _proxy.Int.Read(i, out result);
                    Assert.Equal(RequestState.DataNotFound, result.State);
                    _proxy.Int2.Read(i, out result);
                    Assert.Equal(RequestState.DataNotFound, result.State);
                }

                for (int i = 0; i < count; i++)
                {
                    RequestDescription result = _proxy.Int.CreateSync(i, i);
                    Assert.Equal(RequestState.Complete, result.State);
                    result = _proxy.Int2.CreateSync(i, i);
                    Assert.Equal(RequestState.Complete, result.State);
                }

                for (int i = 0; i < count; i++)
                {
                    RequestDescription result;

                    var value = _proxy.Int.Read(i, out result);
                    Assert.Equal(RequestState.Complete, result.State);
                    Assert.Equal(i, value);
                    value = _proxy.Int2.Read(i, out result);
                    Assert.Equal(i, value);
                }

                Assert.Equal(count, f.Db.Local);
                Assert.Equal(count, f2.Db.Local);

                _proxy.Dispose();
                distr.Dispose();
                storage.Dispose();
            }
        }

        [Theory]
        [InlineData(50)]
        [InlineData(500)]
        public void Proxy_Restore_TwoTablesOneCommand(int count)
        {            
            var filename = nameof(Proxy_Restore_TwoTablesOneCommand);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(countReplics: 1, hash: filename);

                var distr = DistributorApi(DistributorConfiguration(filename, 1), distrServer1, distrServer12);
                var storage1 = WriterApi(StorageConfiguration(filename, 1), storageServer1);
                var storage2 = WriterApi(StorageConfiguration(filename, 1), storageServer2);

                storage1.Module = new TestInjectionModule();
                storage1.Build();

                storage2.Module = new TestInjectionModule();
                storage2.Build();

                distr.Module = new TestInjectionModule();
                distr.Build();

                var f = new TestInMemoryDbFactory(_kernel);
                var f2 = new TestInMemoryDbFactory(_kernel, "Int2");
                storage1.AddDbModule(f);
                storage1.AddDbModule(f2);

                var f3 = new TestInMemoryDbFactory(_kernel);
                var f4 = new TestInMemoryDbFactory(_kernel, "Int2");
                storage2.AddDbModule(f3);
                storage2.AddDbModule(f4);

                storage1.Start();
                _proxy.Start();
                distr.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer1);

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
                    Assert.Equal(RequestState.Complete, result.State);
                    Assert.Equal(i, value);
                    value = _proxy.Int2.Read(i, out result);
                    Assert.Equal(i, value);
                }

                Assert.Equal(count, f.Db.Local + f.Db.Remote);
                Assert.Equal(count, f2.Db.Local + f2.Db.Remote);

                storage2.Start();
                storage2.Api.Restore(RestoreMode.SimpleRestoreNeed);

                Thread.Sleep(TimeSpan.FromMilliseconds(2000));

                Assert.Equal(count, f.Db.Local + f3.Db.Local);
                Assert.Equal(count, f2.Db.Local + f4.Db.Local);

                _proxy.Dispose();
                distr.Dispose();
                storage1.Dispose();
                storage2.Dispose();
            }
        }

        [Theory]
        [InlineData(50)]
        [InlineData(500)]
        public void Proxy_HashFromValue(int count)
        {
            var filename = nameof(Proxy_HashFromValue);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                CreateHashFile(filename, 1);
                CreateConfigFile(countReplics: 1, hash: filename);

                var distr = DistributorApi(DistributorConfiguration(filename, 1), distrServer1, distrServer12);
                var storage = WriterApi(StorageConfiguration(filename, 1), storageServer1);

                storage.Module = new TestInjectionModule();
                storage.Build();

                distr.Module = new TestInjectionModule();
                distr.Build();

                var f = new TestInMemoryDbFactory(_kernel, "Int3", new IntHashConvertor());

                storage.AddDbModule(f);

                storage.Start();
                _proxy.Start();
                distr.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer1);

                for (int i = 0; i < count; i++)
                {
                    RequestDescription result;

                    _proxy.Int3.Read(i, out result);
                    Assert.Equal(RequestState.DataNotFound, result.State);
                }

                for (int i = 0; i < count; i++)
                {
                    RequestDescription result = _proxy.Int3.CreateSync(i, i);
                    Assert.Equal(RequestState.Complete, result.State);
                }

                for (int i = 0; i < count; i++)
                {
                    RequestDescription result;

                    var value = _proxy.Int3.Read(i, out result);
                    Assert.Equal(RequestState.Complete, result.State);
                    Assert.Equal(i, value);
                }

                Assert.Equal(count, f.Db.Local);

                _proxy.Dispose();
                distr.Dispose();
                storage.Dispose();
            }
        }

        [Theory]
        [InlineData(50)]
        [InlineData(500)]
        public void Proxy_Restore_HashFromValue(int count)
        {
            var filename = nameof(Proxy_Restore_HashFromValue);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                CreateHashFile(filename, 2);
                CreateConfigFile(countReplics: 1, hash: filename);

                var distr = DistributorApi(DistributorConfiguration(filename, 1), distrServer1, distrServer12);
                var storage1 = WriterApi(StorageConfiguration(filename, 1), storageServer1);
                var storage2 = WriterApi(StorageConfiguration(filename, 1), storageServer2);

                storage1.Module = new TestInjectionModule();
                storage1.Build();

                storage2.Module = new TestInjectionModule();
                storage2.Build();

                distr.Module = new TestInjectionModule();
                distr.Build();

                var f1 = new TestInMemoryDbFactory(_kernel, "Int3", new IntHashConvertor());
                var f2 = new TestInMemoryDbFactory(_kernel, "Int3", new IntHashConvertor());

                storage1.AddDbModule(f1);
                storage2.AddDbModule(f2);

                storage1.Start();
                _proxy.Start();
                distr.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer1);

                for (int i = 0; i < count; i++)
                {
                    _proxy.Int3.CreateSync(i, i);
                    _proxy.Int3.CreateSync(i, i);
                }

                for (int i = 0; i < count; i++)
                {
                    RequestDescription result;

                    var value = _proxy.Int3.Read(i, out result);
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

                _proxy.Dispose();
                distr.Dispose();
                storage1.Dispose();
                storage2.Dispose();
            }
        }
    }
}

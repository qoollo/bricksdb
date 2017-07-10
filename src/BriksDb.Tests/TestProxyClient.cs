﻿using System;
using Qoollo.Client.Request;
using Qoollo.Tests.NetMock;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestWriter;
using Xunit;

namespace Qoollo.Tests
{
    [Collection("test collection 1")]
    public class TestProxyClient: TestBase
    {
        public TestProxyClient():base()
        {
            _proxy = TestGate(proxyServer);

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
        [InlineData(500)]
        public void ClientProxy_CrudOperations(int count)
        {
            var filename = nameof(ClientProxy_CrudOperations);
            using (new FileCleaner(filename))
            using (new FileCleaner(Impl.Common.Support.Consts.RestoreHelpFile))
            {
                CreateHashFile(filename, 1);

                var distr = DistributorSystem(DistributorCacheConfiguration(10000, 10000000), filename, 1,
                    distrServer12, distrServer1, 10000, 10000);

                var storage = WriterSystem(filename, 2, storageServer1);

                distr.Build(new TestInjectionModule());
                distr.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer12);

                storage.Build(new TestInjectionModule());
                storage.DbModule.AddDbModule(new TestDbInMemory());
                storage.Start();

                for (int i = 0; i < count; i++)
                {
                    var state = _proxy.Int.CreateSync(i, i);
                    Assert.Equal(RequestState.Complete, state.State);
                }

                for (int i = 0; i < count; i++)
                {
                    RequestDescription description;
                    var read = _proxy.Int.Read(i, out description);

                    Assert.Equal(i, read);
                    Assert.Equal(RequestState.Complete, description.State);
                }

                for (int i = 0; i < count; i++)
                {
                    var state = _proxy.Int.DeleteSync(i);
                    Assert.Equal(RequestState.Complete, state.State);
                }

                for (int i = 0; i < count; i++)
                {
                    RequestDescription description;
                    _proxy.Int.Read(i, out description);

                    Assert.Equal(RequestState.DataNotFound, description.State);
                }

                distr.Dispose();
                storage.Dispose();
            }
        }

        [Theory]
        [InlineData(50)]
        [InlineData(500)]
        public void ClientProxy_AsyncRead(int count)
        {
            var filename = nameof(ClientProxy_AsyncRead);
            using (new FileCleaner(filename))
            using (new FileCleaner(Impl.Common.Support.Consts.RestoreHelpFile))
            {
                CreateHashFile(filename, 1);

                var distr = DistributorSystem(DistributorCacheConfiguration(10000, 10000000), filename, 1,
                    distrServer12, distrServer1, 10000, 10000);

                var storage = WriterSystem(filename, 2, storageServer1);

                distr.Build(new TestInjectionModule());
                distr.Start();

                _proxy.Int.SayIAmHere("localhost", distrServer12);

                storage.Build(new TestInjectionModule());
                storage.DbModule.AddDbModule(new TestDbInMemory());
                storage.Start();

                for (int i = 0; i < count; i++)
                {
                    var state = _proxy.Int.CreateSync(i, i);

                    Assert.Equal(RequestState.Complete, state.State);
                }

                for (int i = 0; i < count; i++)
                {
                    var read = _proxy.Int.ReadAsync(i);

                    read.Wait();

                    Assert.Equal(i, read.Result.Value);
                    Assert.Equal(RequestState.Complete, read.Result.RequestDescription.State);
                }

                for (int i = 0; i < count; i++)
                {
                    var state = _proxy.Int.DeleteSync(i);
                    Assert.Equal(RequestState.Complete, state.State);
                }

                for (int i = 0; i < count; i++)
                {
                    var read = _proxy.Int.ReadAsync(i);

                    read.Wait();
                    Assert.Equal(RequestState.DataNotFound, read.Result.RequestDescription.State);
                }

                distr.Dispose();
                storage.Dispose();
            }
        }
    }
}

using System;
using System.Threading;
using Qoollo.Client.Request;
using Qoollo.Impl.Common.Support;
using Qoollo.Tests.NetMock;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestWriter;
using Xunit;

namespace Qoollo.Tests
{
    [Collection("test collection 1")]
    public class TestDistributorClient : TestBase
    {
        [Theory]
        [InlineData(50)]
        [InlineData(500)]
        public void DistributorApi_ProcessAsyncOperationsFromProxy(int count)
        {
            var filename = nameof(DistributorApi_ProcessAsyncOperationsFromProxy);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                CreateHashFile(filename, 1);
                CreateConfigFile(countReplics: 1, hash: filename);

                var proxy = TestGate(proxyServer, 600);
                var distr = DistributorApi(DistributorConfiguration(filename, 1), distrServer1, distrServer12);
                var storage = WriterApi(StorageConfiguration(filename, 1), storageServer1);

                proxy.Module = new TestInjectionModule();
                proxy.Build();
                proxy.Start();

                distr.Module = new TestInjectionModule();
                distr.Build();
                distr.Start();

                proxy.Int.SayIAmHere("localhost", distrServer1);

                storage.Module = new TestInjectionModule();
                storage.Build();
                storage.AddDbModule(new TestInMemoryDbFactory(_kernel));
                storage.Start();

                for (int i = 0; i < count; i++)
                {
                    var state = proxy.Int.CreateSync(i, i);
                    Assert.Equal(RequestState.Complete, state.State);
                }

                for (int i = 0; i < count; i++)
                {
                    RequestDescription description;
                    var read = proxy.Int.Read(i, out description);

                    Assert.Equal(i, read);
                    Assert.Equal(RequestState.Complete, description.State);
                }

                for (int i = 0; i < count; i++)
                {
                    var state = proxy.Int.DeleteSync(i);
                    Assert.Equal(RequestState.Complete, state.State);
                }

                for (int i = 0; i < count; i++)
                {
                    RequestDescription description;
                    proxy.Int.Read(i, out description);

                    Assert.Equal(RequestState.DataNotFound, description.State);
                }

                proxy.Dispose();
                distr.Dispose();
                storage.Dispose();
            }
        }

        [Theory]
        [InlineData(50)]
        [InlineData(500)]
        public void DistributorApi_ProcessSyncOperationsFromProxy(int count)
        {
            var filename = nameof(DistributorApi_ProcessSyncOperationsFromProxy);
            using (new FileCleaner(filename))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                CreateHashFile(filename, 1);
                CreateConfigFile(countReplics: 1, hash: filename);

                var proxy = TestGate(proxyServer, 600);
                var distr = DistributorApi(DistributorConfiguration(filename, 1), distrServer1, distrServer12);
                var storage = WriterApi(StorageConfiguration(filename, 1), storageServer1);

                proxy.Module = new TestInjectionModule();
                proxy.Build();
                proxy.Start();

                distr.Module = new TestInjectionModule();
                distr.Build();
                distr.Start();

                proxy.Int.SayIAmHere("localhost", distrServer1);

                storage.Module = new TestInjectionModule();
                storage.Build();
                storage.AddDbModule(new TestInMemoryDbFactory(_kernel));
                storage.Start();

                for (int i = 0; i < count; i++)
                {
                    proxy.Int.CreateAsync(i, i);
                }

                Thread.Sleep(2000);

                for (int i = 0; i < count; i++)
                {
                    RequestDescription description;
                    var read = proxy.Int.Read(i, out description);

                    Assert.Equal(i, read);
                    Assert.Equal(RequestState.Complete, description.State);
                }

                for (int i = 0; i < count; i++)
                {
                    proxy.Int.DeleteAsync(i);
                }

                Thread.Sleep(2000);

                for (int i = 0; i < count; i++)
                {
                    RequestDescription description;
                    proxy.Int.Read(i, out description);

                    Assert.Equal(RequestState.DataNotFound, description.State);
                }

                proxy.Dispose();
                distr.Dispose();
                storage.Dispose();
            }
        }
    }
}

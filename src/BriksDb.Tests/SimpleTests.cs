using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DistributorModules.Caches;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestModules;
using Xunit;

namespace Qoollo.Tests
{
    [Collection("test collection 1")]
    public class SimpleTests: TestBase
    {
        #region Test cache

        [Fact]
        public void Cache_AddGetUpdateRemove()
        {
            var cache = new TestCache(TimeSpan.FromMilliseconds(10000000));
            var ev1 = new InnerData(new Transaction("123", "123"));
            var ev2 = new InnerData(new Transaction("123", "123123"));
            const string key = "123";

            cache.AddToCache(key, ev1);
            cache.AddToCache(key, ev2);
            var data = cache.Get(key);
            Assert.Equal(0, cache.CountCallback);
            cache.Update(key, data, TimeSpan.FromMinutes(1000));
            Assert.Equal(0, cache.CountCallback);
            cache.Remove(key);
            Assert.Equal(0, cache.CountCallback);

            cache.Dispose();
        }

        [Fact]
        public void CachePerformance()
        {
            var ts1 = TimeSpan.FromMilliseconds(400);
            var ts2 = TimeSpan.FromSeconds(1000);
            var cache = new DistributorCache(ts1, ts2);
            var max = new List<long>();
            var min = new List<long>();
            var avg = new List<float>();
            var obj = new List<InnerData>();
            cache.Start();
            const int count = 10000;

            var calc = new StoredDataHashCalculator();
            for (int i = 0; i < count; i++)
            {
                obj.Add(TestHelper.CreateEvent(calc, i + 1));
            }

            long v1 = 0, v2 = 0;
            float v3 = 0;
            TestCachePerfHelper(cache, obj, 100, ref v1, ref v2, ref v3);
            cache.Dispose();
            min.Add(v1);
            max.Add(v2);
            avg.Add(v3);

            cache = new DistributorCache(ts1, ts2);
            cache.Start();
            TestCachePerfHelper(cache, obj, 1000, ref v1, ref v2, ref v3);
            cache.Dispose();
            min.Add(v1);
            max.Add(v2);
            avg.Add(v3);

            cache = new DistributorCache(ts1, ts2);
            cache.Start();
            TestCachePerfHelper(cache, obj, 10000, ref v1, ref v2, ref v3);
            cache.Dispose();
            min.Add(v1);
            max.Add(v2);
            avg.Add(v3);

            cache.Dispose();
        }

        private void TestCachePerfHelper(DistributorCache cache, List<InnerData> obj, int count, ref long min,
                                         ref long max, ref float avg)
        {
            var sp = new Stopwatch();
            min = int.MaxValue;
            max = 0;
            avg = 0;
            for (int i = 0; i < count; i++)
            {
                cache.AddToCache(obj[i].Transaction.DataHash, obj[i].Transaction);
            }

            for (int i = 0; i < count; i++)
            {
                sp.Start();
                cache.Update(obj[i].Transaction.DataHash, obj[i].Transaction);
                sp.Stop();
                long mls = sp.ElapsedMilliseconds;
                avg += mls;

                if (min > mls) min = mls;
                if (max < mls) max = mls;
                sp.Reset();
            }
            avg = avg / count;
        }

        [Fact]
        public void Cache_AddGet()
        {
            var cache = new TestCache(TimeSpan.FromMilliseconds(100));

            var ev = new InnerData(new Transaction("123", ""))
            {
                DistributorData = new DistributorData{ Destination = new List<ServerId>() }
            };

            cache.AddToCache("123", ev);
            var ret = cache.Get("123");
            Assert.Equal(ev, ret);
            Thread.Sleep(200);
            ret = cache.Get("123");
            Assert.Equal(null, ret);
            cache.AddToCache("123", ev);
            ret = cache.Get("123");
            Assert.Equal(ev, ret);

            cache.AddToCache("1234", ev);
            ret = cache.Get("1234");
            Assert.Equal(ev, ret);
            Thread.Sleep(200);
            ret = cache.Get("1234");
            Assert.Equal(null, ret);

            cache.Dispose();
        }

        #endregion

        #region Test async tasks

        [Fact]
        public void AsyncTaskModule_AddAsyncTask_AmountOfOperations()
        {
            var test = new AsyncTaskModule(null, new QueueConfiguration(2, -1));
            int value = 0;
            const string name1 = "test1";
            var async1 = new AsyncDataPeriod(TimeSpan.FromMilliseconds(500), async => Interlocked.Increment(ref value),
                                             name1, -1);
            test.Start();

            test.AddAsyncTask(async1, false);
            Thread.Sleep(TimeSpan.FromMilliseconds(600));
            Assert.Equal(1, value);

            test.StopTask(name1);
            Thread.Sleep(TimeSpan.FromMilliseconds(600));
            Assert.Equal(1, value);

            test.RestartTask(name1, true);
            Thread.Sleep(TimeSpan.FromMilliseconds(700));
            Assert.Equal(3, value);

            test.Dispose();
        }

        [Fact]
        public void AsyncTaskModule_AddAsyncTask_AmountOfOperations_2Tasks()
        {
            var test = new AsyncTaskModule(null, new QueueConfiguration(2, -1));
            int value = 0;
            const string name1 = "test1";
            const string name2 = "test2";
            var async1 = new AsyncDataPeriod(TimeSpan.FromMilliseconds(500), async => Interlocked.Increment(ref value),
                                             name1, -1);
            var async2 = new AsyncDataPeriod(TimeSpan.FromMilliseconds(500), async => Interlocked.Increment(ref value),
                                             name2, -1);
            test.Start();

            test.AddAsyncTask(async1, true);
            test.AddAsyncTask(async2, true);
            Thread.Sleep(TimeSpan.FromMilliseconds(300));
            Assert.Equal(2, value);

            Thread.Sleep(TimeSpan.FromMilliseconds(500));
            Assert.Equal(4, value);

            test.Dispose();
        }

        [Fact]
        public void AsyncTaskModule_Dispose_StopAsyncTaskAfterNumberOfRetry()
        {
            var test = new AsyncTaskModule(null, new QueueConfiguration(2, -1));
            int value = 0;
            const string name1 = "test1";
            var async1 = new AsyncDataPeriod(TimeSpan.FromMilliseconds(100), async => Interlocked.Increment(ref value),
                                             name1, 4);
            test.Start();

            test.AddAsyncTask(async1, true);
            Thread.Sleep(TimeSpan.FromMilliseconds(800));
            Assert.Equal(4, value);

            test.Dispose();
        }

        [Fact]
        public void AsyncTaskModule_PingServers_AvalilableAfterSomeTime()
        {
            var filename = nameof(AsyncTaskModule_PingServers_AvalilableAfterSomeTime);
            using (new FileCleaner(filename))
            {
                CreateHashFile(filename, 2);

                #region hell

                var dnet = DistributorNetModule();
                var ddistributor = DistributorDistributorModule(filename, 1, dnet);
                dnet.SetDistributor(ddistributor);

                dnet.Start();
                ddistributor.Start();
                GlobalQueue.Queue.Start();

                #endregion

                var data1 = new InnerData(new Transaction("", "default"));
                var data2 = new InnerData(new Transaction("eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee", "default"));

                var dest = ddistributor.GetDestination(data1, false);
                var dest2 = ddistributor.GetDestination(data2, false);

                dnet.Process(dest.First(), data1);
                dnet.Process(dest2.First(), data1);

                Thread.Sleep(100);

                dest = ddistributor.GetDestination(data1, false);
                dest2 = ddistributor.GetDestination(data2, false);

                Assert.Equal(null, dest);
                Assert.Equal(null, dest2);

                var h1 = TestHelper.OpenWriterHost(storageServer1);
                var h2 = TestHelper.OpenWriterHost(storageServer2);

                Thread.Sleep(TimeSpan.FromMilliseconds(800));

                dest = ddistributor.GetDestination(data1, false);
                dest2 = ddistributor.GetDestination(data2, false);

                Assert.NotEqual(null, dest);
                Assert.NotEqual(null, dest2);

                Assert.Equal(1, dest.Count);
                Assert.Equal(1, dest2.Count);

                GlobalQueue.Queue.Dispose();

                ddistributor.Dispose();
                h1.Dispose();
                h2.Dispose();
            }
        }

        #endregion
    }
}

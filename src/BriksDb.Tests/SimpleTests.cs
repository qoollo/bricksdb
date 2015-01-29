using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Client.Support;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DistributorModules;
using Qoollo.Impl.DistributorModules.Caches;
using Qoollo.Impl.DistributorModules.DistributorNet;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestModules;

namespace Qoollo.Tests
{
    [TestClass]
    public class SimpleTests
    {
        #region Test cache

        [TestMethod]
        public void Cache_AddGetUpdateRemove()
        {
            var cache = new TestCache(TimeSpan.FromMilliseconds(10000000));
            var ev1 = new InnerData(new Transaction("123", "123"));
            var ev2 = new InnerData(new Transaction("123", "123123"));
            const string key = "123";

            cache.AddToCache(key, ev1);
            cache.AddToCache(key, ev2);
            var data = cache.Get(key);
            Assert.AreEqual(0, cache.CountCallback);
            cache.Update(key, data, TimeSpan.FromMinutes(1000));
            Assert.AreEqual(0, cache.CountCallback);
            cache.Remove(key);
            Assert.AreEqual(0, cache.CountCallback);
        }

        [TestMethod]
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
                cache.AddToCache(obj[i].Transaction.EventHash, obj[i].Transaction);
            }

            for (int i = 0; i < count; i++)
            {
                sp.Start();
                cache.Update(obj[i].Transaction.EventHash, obj[i].Transaction);
                sp.Stop();
                long mls = sp.ElapsedMilliseconds;
                avg += mls;

                if (min > mls) min = mls;
                if (max < mls) max = mls;
                sp.Reset();
            }
            avg = avg / count;
        }

        [TestMethod]
        public void Cache_AddGet()
        {
            var cache = new TestCache(TimeSpan.FromMilliseconds(100));

            var ev = new InnerData(new Transaction("123", ""))
            {
                Transaction = { Destination = new List<ServerId>() }
            };

            cache.AddToCache("123", ev);
            var ret = cache.Get("123");
            Assert.AreEqual(ev, ret);
            Thread.Sleep(200);
            ret = cache.Get("123");
            Assert.AreEqual(null, ret);
            cache.AddToCache("123", ev);
            ret = cache.Get("123");
            Assert.AreEqual(ev, ret);

            cache.AddToCache("1234", ev);
            ret = cache.Get("1234");
            Assert.AreEqual(ev, ret);
            Thread.Sleep(200);
            ret = cache.Get("1234");
            Assert.AreEqual(null, ret);
        }

        #endregion

        #region Test async tasks

        [TestMethod]
        public void AsyncTaskModule_AddAsyncTask_AmountOfOperations()
        {
            var test = new AsyncTaskModule(new QueueConfiguration(2, -1));
            int value = 0;
            const string name1 = "test1";
            var async1 = new AsyncDataPeriod(TimeSpan.FromMilliseconds(500), async => Interlocked.Increment(ref value),
                                             name1, -1);
            test.Start();

            test.AddAsyncTask(async1, false);
            Thread.Sleep(TimeSpan.FromMilliseconds(600));
            Assert.AreEqual(1, value);

            test.StopTask(name1);
            Thread.Sleep(TimeSpan.FromMilliseconds(600));
            Assert.AreEqual(1, value);

            test.RestartTask(name1, true);
            Thread.Sleep(TimeSpan.FromMilliseconds(700));
            Assert.AreEqual(3, value);

            test.Dispose();

        }

        [TestMethod]
        public void AsyncTaskModule_AddAsyncTask_AmountOfOperations_2Tasks()
        {
            var test = new AsyncTaskModule(new QueueConfiguration(2, -1));
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
            Assert.AreEqual(2, value);

            Thread.Sleep(TimeSpan.FromMilliseconds(500));
            Assert.AreEqual(4, value);

            test.Dispose();
        }

        [TestMethod]
        public void AsyncTaskModule_Dispose_StopAsyncTaskAfterNumberOfRetry()
        {
            var test = new AsyncTaskModule(new QueueConfiguration(2, -1));
            int value = 0;
            const string name1 = "test1";
            var async1 = new AsyncDataPeriod(TimeSpan.FromMilliseconds(100), async => Interlocked.Increment(ref value),
                                             name1, 4);
            test.Start();

            test.AddAsyncTask(async1, true);
            Thread.Sleep(TimeSpan.FromMilliseconds(800));
            Assert.AreEqual(4, value);

            test.Dispose();
        }

        [TestMethod]
        public void AsyncTaskModule_PingServers_AvalilableAfterSomeTime()
        {
            const int storageServer1 = 22131;
            const int storageServer2 = 22132;
            const int distrServer1 = 22134;
            const int distrServer12 = 23134;

            var writer = new HashWriter(new HashMapConfiguration("TestAsyncPing", HashMapCreationMode.CreateNew, 2, 3, HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", storageServer1, 157);
            writer.SetServer(1, "localhost", storageServer2, 157);
            writer.Save();

            #region hell

            var connection = new ConnectionConfiguration("testService", 10);

            var distrconfig = new DistributorHashConfiguration(1);
            var queueconfig = new QueueConfiguration(1, 100);
            var dnet = new DistributorNetModule(connection,
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            var ddistributor = new DistributorModule(new AsyncTasksConfiguration(TimeSpan.FromMilliseconds(200)),
                new AsyncTasksConfiguration(TimeSpan.FromMilliseconds(200)),
                distrconfig,
                queueconfig, dnet,
                new ServerId("localhost", distrServer1),
                new ServerId("localhost", distrServer12),
                new HashMapConfiguration("TestAsyncPing", HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Distributor));
            dnet.SetDistributor(ddistributor);

            dnet.Start();
            ddistributor.Start();
            GlobalQueue.Queue.Start();

            #endregion

            var data1 = new InnerData(new Transaction("", ""));
            var data2 = new InnerData(new Transaction("eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee", ""));

            var dest = ddistributor.GetDestination(data1, false);
            var dest2 = ddistributor.GetDestination(data2, false);

            dnet.Process(dest.First(), data1);
            dnet.Process(dest2.First(), data1);

            Thread.Sleep(100);

            dest = ddistributor.GetDestination(data1, false);
            dest2 = ddistributor.GetDestination(data2, false);

            Assert.AreEqual(null, dest);
            Assert.AreEqual(null, dest2);

            TestHelper.OpenWriterHost(new ServerId("localhost", storageServer1),
                new ConnectionConfiguration("testService", 10));
            TestHelper.OpenWriterHost(new ServerId("localhost", storageServer2),
                new ConnectionConfiguration("testService", 10));

            Thread.Sleep(TimeSpan.FromMilliseconds(800));

            Assert.AreEqual(1, ddistributor.GetDestination(data1, false).Count);
            Assert.AreEqual(1, ddistributor.GetDestination(data2, false).Count);

            ddistributor.Dispose();
        }

        #endregion
    }
}

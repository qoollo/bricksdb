using System;
using System.Threading;
using System.Threading.Tasks;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Cache;
using Xunit;

namespace Qoollo.Tests
{
    [Collection("test collection 1")]
    public class TestTransactionLogic:TestBase
    {
         class TestData
        {
            public int Counter;
            public DistributorData DistributorData;
        }

        class TestCache : CacheModule<TestData>
        {
            private readonly TimeSpan _aliveTimeout;

            public Action<InnerData> DataTimeout;

            public TestCache(DistributorCacheConfiguration cacheConfiguration)
                : base(cacheConfiguration.TimeAliveBeforeDeleteMls)
            {                
                _aliveTimeout = cacheConfiguration.TimeAliveAfterUpdateMls;
            }

            public void Update(string key, TestData obj)
            {
                Remove(key);
                AddAliveToCache(key, obj, _aliveTimeout);
            }       

             protected override void RemovedCallback(string key, TestData obj)
             {
                 throw new NotImplementedException();
             }
        }

        [Fact]
        public void DistributorData_TestCacheLock_TwoThread_IncrementCounter()
        {
            const string key = "123";
            var cache = new TestCache(new DistributorCacheConfiguration(TimeSpan.FromMinutes(10),
                    TimeSpan.FromMinutes(10)));

            var data = new TestData {DistributorData = new DistributorData()};
            cache.AddToCache(key, data);

            var action = new Action(() =>
            {
                var value = cache.Get(key);
                using (value.DistributorData.GetLock())
                {
                    value.Counter++;
                    cache.Update(key, value);
                }
            });

            Task.Factory.StartNew(action);
            Task.Factory.StartNew(action);

            Thread.Sleep(1000);
            Assert.Equal(2, cache.Get(key).Counter);

            cache.Dispose();
        }
    }
}

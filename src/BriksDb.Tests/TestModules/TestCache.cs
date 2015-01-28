using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Modules.Cache;

namespace Qoollo.Tests.TestModules
{
    class TestCache : CacheModule<InnerData>
    {
        public int CountCallback = 0;
        public TestCache(TimeSpan timeout)
            : base(timeout)
        {
        }

        protected override void RemovedCallback(string key, InnerData obj)
        {
            Interlocked.Increment(ref CountCallback);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using Qoollo.Impl.Collector.Load;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Collector.Tasks;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Tests.TestCollector
{
    class TestSelectTask : SearchTask
    {
        public List<SearchData> Data;
        public int CountLoads;
        public bool Finish;

        public TestSelectTask(string tableName, List<ServerId> servers, string script, FieldDescription keyDescription)
            : base(servers, keyDescription, script, new List<FieldDescription>(), tableName)
        {
            Data = new List<SearchData>();
            CountLoads = 0;
            Finish = false;
        }

        public override List<SearchData> GetData()
        {
            var ret = new List<SearchData>(Data);
            return ret;
        }

        protected override bool BackgroundLoad(IDataLoader loader, Func<OrderSelectTask, List<SingleServerSearchTask>, List<SearchData>> func)
        {
            Interlocked.Increment(ref CountLoads);
            return Finish;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Client.Support;

namespace Qoollo.Benchmark.Send
{
    internal class DataProvider : CommonDataProvider<long, string>
    {
        public override string CalculateHashFromKey(long key)
        {
            return key.ToString();
        }
    }
}

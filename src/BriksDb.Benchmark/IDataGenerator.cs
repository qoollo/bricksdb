using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Benchmark
{
    interface IDataGenerator
    {
        IEnumerable<string> GenerateData(int count);
    }
}

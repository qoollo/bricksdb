using System.Collections.Generic;

namespace Qoollo.Benchmark.DataGenerator
{
    interface IDataGenerator
    {
        IEnumerable<string> GenerateData(int count);
    }
}

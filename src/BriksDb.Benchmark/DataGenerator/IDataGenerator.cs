using System.Collections.Generic;

namespace Qoollo.Benchmark.DataGenerator
{
    public interface IDataGenerator
    {
        IEnumerable<string> GenerateData(int count);
    }
}

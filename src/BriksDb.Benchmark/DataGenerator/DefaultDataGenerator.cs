using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Benchmark.DataGenerator
{
    class DefaultDataGenerator:IDataGenerator
    {
        public IEnumerable<string> GenerateData(int count)
        {
            var data = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                data.Add("xxx");
            }
            return data;
        }
    }
}

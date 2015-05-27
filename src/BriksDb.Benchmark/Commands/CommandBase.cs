using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Concierge.Attributes;
using Qoollo.Concierge.Commands;

namespace Qoollo.Benchmark.Commands
{
    public class CommandBase : UserCommand
    {
        [Parameter(ShortKey = 'c', IsRequired = false, Description = "Count threads", DefaultValue = 1)]
        public int ThreadsCount { get; set; }

        [Parameter(ShortKey = 't', IsRequired = false, Description = "Table name", DefaultValue = "BenchmarkTable")]
        public string TableName { get; set; }
    }
}

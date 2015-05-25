using Qoollo.Concierge.Attributes;
using Qoollo.Concierge.Commands;

namespace Qoollo.Benchmark
{
    internal class BenchmarkCommand : UserCommand
    {
        [Parameter(ShortKey = 't', IsRequired = false, Description = "Table name", DefaultValue = "BenchmarkTable")]
        public string TableName { get; set; }

        [Parameter(ShortKey = 'n', IsRequired = false, Description = "Count data", DefaultValue = -1)]
        public int DataCount { get; set; }

        [Parameter(ShortKey = 'l', IsRequired = true, Description = "Test type")]
        public string TestType { get; set; }

        [Parameter(ShortKey = 'h', IsRequired = true, Description = "Connection host")]
        public string Host { get; set; }

        [Parameter(ShortKey = 'p', IsRequired = true, Description = "Connection port")]
        public int Port { get; set; }

        [Parameter(ShortKey = 'c', IsRequired = false, Description = "Count threads", DefaultValue = 1)]
        public int ThreadsCount { get; set; }

        [Parameter(ShortKey = 'r', IsRequired = false, Description = "KeyRange", DefaultValue = 1000000)]
        public int KeyRange { get; set; }

        [Parameter(ShortKey = 'g', IsRequired = false, Description = "Generator type", DefaultValue = "default")]
        public string Generator { get; set; }
    }
}
using Qoollo.Concierge.Attributes;

namespace Qoollo.Benchmark.Commands
{
    internal class WriterCommand : CommandBase
    {
        [Parameter(ShortKey = 'l', IsRequired = true, Description = "Test type")]
        public string TestType { get; set; }        

        [Parameter(ShortKey = 'n', IsRequired = false, Description = "Count data", DefaultValue = -1)]
        public int DataCount { get; set; }        

        [Parameter(ShortKey = 'r', IsRequired = false, Description = "KeyRange", DefaultValue = 1000000)]
        public long KeyRange { get; set; }

        [Parameter(ShortKey = 'g', IsRequired = false, Description = "Generator type", DefaultValue = "default")]
        public string Generator { get; set; }

        [Parameter(ShortKey = 'h', IsRequired = true, Description = "Connection host")]
        public string Host { get; set; }

        [Parameter(ShortKey = 'p', IsRequired = true, Description = "Connection port")]
        public int Port { get; set; }
    }
}
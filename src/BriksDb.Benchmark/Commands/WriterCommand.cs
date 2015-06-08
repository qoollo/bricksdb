using Qoollo.Concierge.Attributes;

namespace Qoollo.Benchmark.Commands
{
    public class WriterCommand : CommandBase
    {
        [Parameter(ShortKey = 'l', IsRequired = true, Description = "Test type")]
        public string TestType { get; set; }        

        [Parameter(ShortKey = 'n', IsRequired = false, Description = "Count data", DefaultValue = -1)]
        public int DataCount { get; set; }        

        [Parameter(ShortKey = 'r', IsRequired = false, Description = "KeyRange", DefaultValue = 1000000)]
        public long KeyRange { get; set; }

        [Parameter(ShortKey = 'g', IsRequired = false, Description = "Generator type", DefaultValue = "default")]
        public string Generator { get; set; }

        [Parameter(ShortKey = 'h', IsRequired = true, Description = "Remote connection host")]
        public string Host { get; set; }

        [Parameter(ShortKey = 'p', IsRequired = true, Description = "Remote connection port")]
        public int Port { get; set; }

        [Parameter(ShortKey = 'k', IsRequired = false, Description = "Local connection host")]
        public string Localhost { get; set; }

        [Parameter(ShortKey = 'i', IsRequired = false, Description = "Local connection port")]
        public int Localport { get; set; }
    }
}
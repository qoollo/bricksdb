﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Concierge.Attributes;

namespace Qoollo.Benchmark.Commands
{
    public class CollectorCommand : CommandBase
    {
        [Parameter(ShortKey = 'q', IsRequired = true, Description = "File with queries")]
        public string FileName { get; set; }

        [Parameter(ShortKey = 'f', IsRequired = false, Description = "Hash file name",
            DefaultValue = "ServersHashFile")]
        public string HashFileName { get; set; }

        [Parameter(ShortKey = 'r', IsRequired = false, Description = "Count replics", DefaultValue = 1)]
        public int CountReplics { get; set; }

        [Parameter(ShortKey = 's', IsRequired = false, Description = "Page size", DefaultValue = 100)]
        public int PageSize { get; set; }

        [Parameter(ShortKey = 'h', IsRequired = false, Description = "Distributor host", DefaultValue = "default")]
        public string Host { get; set; }

        [Parameter(ShortKey = 'p', IsRequired = false, Description = "Distributor hort", DefaultValue = -1)]
        public int Port { get; set; }
    }
}

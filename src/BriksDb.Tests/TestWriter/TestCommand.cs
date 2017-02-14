using System;
using System.Collections.Generic;

namespace Qoollo.Tests.TestWriter
{
    public class TestCommand: IDisposable
    {
        public string Command { get; set; }

        public int Key { get; set; }

        public List<int> Keys { get; set; }

        public bool Local { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime DeleteTime { get; set; }

        public int Support { get; set; }

        public string Hash { get; set; }

        public void Dispose()
        {
        }
    }
}
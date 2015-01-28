using System;

namespace Qoollo.Tests.TestWriter
{
    public class TestCommand
    {
        public string Command { get; set; }

        public int Value { get; set; }

        public bool Local { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime DeleteTime { get; set; }

        public int Support { get; set; }
    }
}
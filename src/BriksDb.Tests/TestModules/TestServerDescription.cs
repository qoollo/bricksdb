using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Tests.TestModules
{
    class TestServerDescription : ServerId
    {
        public int Id { get; set; }

        public TestServerDescription(int id)
            : base("test", 100)
        {
            Id = id;
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Client.WriterGate;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Impl.Writer.Db;

namespace Qoollo.Tests.TestWriter
{
    internal class TestInMemoryDbFactory : DbFactory
    {
        private string _tableName = "";
        private IHashCalculater _hashCalculater = null;

        public TestDbInMemory Db;
        public override DbModule Build()
        {
            Db = _hashCalculater != null
                ? new TestDbInMemory(_tableName, _hashCalculater)
                : new TestDbInMemory(_tableName);
            return Db;
        }

        public TestInMemoryDbFactory()
        {
        }

        public TestInMemoryDbFactory(string tableName)
        {
            _tableName = tableName;
        }

        public TestInMemoryDbFactory(string tableName, IHashCalculater hashCalculater)
        {
            _tableName = tableName;
            _hashCalculater = hashCalculater;
        }

        public override ScriptParser GetParser()
        {
            throw new NotImplementedException();
        }
    }
}

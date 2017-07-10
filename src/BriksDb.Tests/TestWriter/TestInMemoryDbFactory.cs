using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ninject;
using Qoollo.Client.WriterGate;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Impl.Writer.Db;

namespace Qoollo.Tests.TestWriter
{
    internal class TestInMemoryDbFactory : DbFactory
    {
        private readonly StandardKernel _kernel;
        private string _tableName = "";
        private IHashCalculater _hashCalculater = null;

        public TestDbInMemory Db;
        public override DbModule Build()
        {
            //todo new init
            Db = _hashCalculater != null
                ? new TestDbInMemory(_tableName, _hashCalculater)
                : new TestDbInMemory(_tableName);
            return Db;
        }

        public TestInMemoryDbFactory(StandardKernel kernel)
        {
            _kernel = kernel;
        }

        public TestInMemoryDbFactory(StandardKernel kernel, string tableName)
        {
            _kernel = kernel;
            _tableName = tableName;
        }

        public TestInMemoryDbFactory(StandardKernel kernel, string tableName, IHashCalculater hashCalculater)
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

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Impl.Writer.Db;
using Qoollo.Tests.Support;

namespace Qoollo.Tests.TestWriter
{
    internal class TestDbInMemory : DbLogicModule<TestCommand, int, int, TestCommand, TestDbReader>
    {
        private TestDbImplModule _impl;

        public TestDbInMemory(string tableName, IHashCalculater hashCalculater)
            : base(
                hashCalculater, true,
                new TestUserCommandCreator(tableName),
                new TestMetaDataCommandCreator(), TestDbHelper.NewInstance
                )
        {
            _impl = TestDbHelper.Instance;
        }

        public TestDbInMemory(string tableName)
            : base(
                new IntHashConvertor(), false,
                new TestUserCommandCreator(tableName),
                new TestMetaDataCommandCreator(), TestDbHelper.NewInstance
                )
        {
            _impl = TestDbHelper.Instance;
        }

        public TestDbInMemory()
            : base(
                new IntHashConvertor(), false,
                new TestUserCommandCreator(),
                new TestMetaDataCommandCreator(), TestDbHelper.NewInstance
                )
        {
            _impl = TestDbHelper.Instance;
        }

        public int Local { get { return _impl.Local; } }
        public int Remote { get { return _impl.Remote; } }
        public int Deleted { get { return _impl.Deleted; } }
    }

    internal static class TestDbHelper
    {
        private static TestDbImplModule _impl;
        private static object _lock = new object();

        public static TestDbImplModule Instance
        {
            get
            {
                if (_impl == null)
                {
                    lock (_lock)
                    {
                        if (_impl == null)
                        {
                            _impl = new TestDbImplModule();
                        }
                    }
                }
                return _impl;
            }
        }

        public static TestDbImplModule NewInstance
        {
            get
            {
                _impl = new TestDbImplModule();
                return _impl;
            }
        }

        public static void SetData(TestDbImplModule impl)
        {
            _impl = impl;
        }
    }
}

using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Postgre;
using Qoollo.Tests.Support;

namespace Qoollo.Tests
{
    [TestClass]
    public class PostgreTest
    {
        private const string ConnectionString = @"Provider=PostgreSQL OLE DB Provider;
                                                    Data Source=10.5.6.112;
                                                    location=postgres; User ID=myUsername;password=myPassword;timeout=1000;";
        private TestWriterGate _writer1;

        [TestInitialize]
        public void Initialize()
        {
            _writer1 = new TestWriterGate();
        }
        [TestMethod]
        public void TestPostgreMethod1()
        {
            var writer =
                new HashWriter(new HashMapConfiguration("TestPostgreMethod1", HashMapCreationMode.CreateNew, 1, 1,
                    HashFileType.Writer));
            writer.CreateMap();
            writer.SetServer(0, "localhost", 157, 157);
            writer.Save();

            _writer1.Build(157, "TestPostgreMethod1", 1);
            _writer1.Db.AddDbModule(new PostgreDbFactory<int, StoredData>(new StoredDataDataProvider(),
                new StoredDataCommandCreator(), new PostgreConnectionParams(ConnectionString, 1, 1), false).Build());
        }
    }
}

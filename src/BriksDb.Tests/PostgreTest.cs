using System;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Client.Support;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Postgre;
using Qoollo.Tests.Support;

namespace Qoollo.Tests
{
    [TestClass]
    public class PostgreTest
    {
        private const string ConnectionString = "Server=10.5.6.112;" +
                                                "Port=5432;" +
                                                "Database=postgres;" +
                                                "User Id=postgres;";
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
            var provider = new StoredDataDataProvider();

            _writer1.Db.AddDbModule(new PostgreDbFactory<int, StoredData>(provider,
                new StoredDataCommandCreator(), new PostgreConnectionParams(ConnectionString, 1, 1), false)
                .Build());

            TestHelper.OpenDistributorHostForDb(new ServerId("localhost", 22188),
                new ConnectionConfiguration("testService", 10));

            _writer1.Start();

            var data = new StoredData(1);

            var ev = new InnerData(new Transaction(provider.CalculateHashFromKey(data.Id), "")
            {
                OperationName = OperationName.Create,
                TableName = "TestStored"
            })
            {
                Data = CommonDataSerializer.Serialize(data),
                Key = CommonDataSerializer.Serialize(data.Id),
                Transaction = {Distributor = new ServerId("localhost", 22188)}
            };

            var result  = _writer1.Input.ProcessSync(ev);

            _writer1.Dispose();
        }
    }
}

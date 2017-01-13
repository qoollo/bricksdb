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
        private const string TableName = "TestStored";
        private const string ConnectionString = "Server=127.0.0.1;" +
                                                "Port=5432;" +
                                                "Database=postgres;" +
                                                "User Id=postgres; Password = 123";

        private static StoredDataDataProvider _storedDataProvider = new StoredDataDataProvider();


        [TestInitialize]
        public void Initialize()
        {
            using (var connection = new Npgsql.NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = $"CREATE TABLE IF NOT EXISTS {TableName} ( Id INTEGER NOT NULL PRIMARY KEY );";
                    cmd.ExecuteNonQuery();
                }
                bool shouldCreateMetaTable = false;
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = $"SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'metatable_{TableName.ToLower()}')";
                    shouldCreateMetaTable = !(bool)cmd.ExecuteScalar();
                }
                if (shouldCreateMetaTable)
                {
                    var metaObj = new Impl.Postgre.Internal.PostgreMetaDataCommandCreator<int, StoredData>(
                        new Impl.Postgre.Internal.PostgreUserCommandCreatorInner<int, StoredData>(new PostgreStoredDataCommandCreator()));
                    metaObj.SetTableName(new List<string>() { TableName });
                    metaObj.SetKeyName("Id");
                    using (var cmd = metaObj.InitMetaDataDb("Meta_Id INTEGER"))
                    {
                        cmd.Connection = connection;
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            Cleanup();
        }

        [TestCleanup]
        public void Cleanup()
        {
            using (var connection = new Npgsql.NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = $"DELETE FROM {TableName}; DELETE FROM MetaTable_{TableName};";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        // ===========

        private static ServerId CreateUniqueServerId()
        {
            return new ServerId("localhost", 22188 + (Thread.CurrentThread.ManagedThreadId % 101));
        }

        private static void CreateHashFileForSingleWriter(string testName)
        {
            var writer = new HashWriter(new HashMapConfiguration(testName, HashMapCreationMode.CreateNew, 1, 1, HashFileType.Writer));
            writer.CreateMap();
            writer.SetServer(0, "localhost", 157, 157);
            writer.Save();
        }

        private static TestWriterGate CreatePostgreWriter(string testName)
        {
            var writer = new TestWriterGate();
            writer.Build(157, testName, 1);
    
            writer.Db.AddDbModule(new PostgreDbFactory<int, StoredData>(_storedDataProvider,
                new PostgreStoredDataCommandCreator(), new PostgreConnectionParams(ConnectionString, 1, 1), false)
                .Build());

            return writer;
        }

        private static InnerData CreateRequest(StoredData data, ServerId distributorServerId = null)
        {
            return new InnerData(new Transaction(_storedDataProvider.CalculateHashFromKey(data.Id), "")
            {
                OperationName = OperationName.Create,
                TableName = TableName
            })
            {
                Data = CommonDataSerializer.Serialize(data),
                Key = CommonDataSerializer.Serialize(data.Id),
                Transaction = { Distributor = distributorServerId ?? CreateUniqueServerId() }
            };
        }

        private static InnerData ReadRequest(int key, ServerId distributorServerId = null)
        {
            return new InnerData(new Transaction(_storedDataProvider.CalculateHashFromKey(key), "")
            { 
                OperationName = OperationName.Read,
                TableName = TableName,
            })
            { 
                Data = null,
                Key = CommonDataSerializer.Serialize(key),
                Transaction = { Distributor = distributorServerId ?? CreateUniqueServerId() }
            };
        }

        // ==============

        [TestMethod]
        public void Postgre_Create_Read_Test()
        {
            CreateHashFileForSingleWriter(nameof(Postgre_Create_Read_Test));
            var writer = CreatePostgreWriter(nameof(Postgre_Create_Read_Test));
            TestHelper.OpenDistributorHostForDb(CreateUniqueServerId(), new ConnectionConfiguration("testService", 10));

            writer.Start();

            var data = new StoredData(1);
            var createRequest = CreateRequest(data);
            var result = writer.Input.ProcessSync(createRequest);
            Assert.IsFalse(result.IsError);


            var readRequest = ReadRequest(data.Id);
            var resultRead = writer.Input.ReadOperation(readRequest);
            Assert.IsFalse(resultRead.Transaction.IsError);
            Assert.IsNotNull(resultRead.Data);
            Assert.AreEqual(data.Id, CommonDataSerializer.Deserialize<StoredData>(resultRead.Data).Id);

            writer.Dispose();
        }
    }
}

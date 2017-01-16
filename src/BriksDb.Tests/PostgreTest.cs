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
using System.Linq;

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
        private static HashWriter CreateHashFileForTwoWriters(string testName)
        {
            var writer = new HashWriter(new HashMapConfiguration(testName, HashMapCreationMode.CreateNew, 2, 1, HashFileType.Writer));
            writer.CreateMap();
            writer.SetServer(0, "localhost", 157, 157);
            writer.SetServer(1, "localhost", 158, 158);
            writer.Save();
            return writer;
        }

        private static TestWriterGate CreatePostgreWriter(string testName, int id = 0)
        {
            var writer = new TestWriterGate();
            writer.Build(157 + id, testName, 1);

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

        private static InnerData UpdateRequest(StoredData data, ServerId distributorServerId = null)
        {
            return new InnerData(new Transaction(_storedDataProvider.CalculateHashFromKey(data.Id), "")
            {
                OperationName = OperationName.Update,
                TableName = TableName
            })
            {
                Data = CommonDataSerializer.Serialize(data),
                Key = CommonDataSerializer.Serialize(data.Id),
                Transaction = { Distributor = distributorServerId ?? CreateUniqueServerId() }
            };
        }

        private static InnerData DeleteRequest(int id, ServerId distributorServerId = null)
        {
            return new InnerData(new Transaction(_storedDataProvider.CalculateHashFromKey(id), "")
            {
                OperationName = OperationName.Delete,
                TableName = TableName
            })
            {
                Data = null,
                Key = CommonDataSerializer.Serialize(id),
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
            //TestHelper.OpenDistributorHostForDb(CreateUniqueServerId(), new ConnectionConfiguration("testService", 10));

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

        [TestMethod]
        public void Postgre_Create_Update_Read_Test()
        {
            CreateHashFileForSingleWriter(nameof(Postgre_Create_Update_Read_Test));
            var writer = CreatePostgreWriter(nameof(Postgre_Create_Update_Read_Test));
            //TestHelper.OpenDistributorHostForDb(CreateUniqueServerId(), new ConnectionConfiguration("testService", 10));

            writer.Start();

            var data = new StoredData(1);
            var createRequest = CreateRequest(data);
            var result = writer.Input.ProcessSync(createRequest);
            Assert.IsFalse(result.IsError);

            var updateData = new StoredData(1);
            var updateRequest = UpdateRequest(updateData);
            var updateResult = writer.Input.ProcessSync(updateRequest);
            Assert.IsFalse(updateResult.IsError);

            var readRequest = ReadRequest(data.Id);
            var resultRead = writer.Input.ReadOperation(readRequest);
            Assert.IsFalse(resultRead.Transaction.IsError);
            Assert.IsNotNull(resultRead.Data);
            Assert.AreEqual(data.Id, CommonDataSerializer.Deserialize<StoredData>(resultRead.Data).Id);

            writer.Dispose();
        }

        [TestMethod]
        public void Postgre_Create_Delete_Test()
        {
            CreateHashFileForSingleWriter(nameof(Postgre_Create_Delete_Test));
            var writer = CreatePostgreWriter(nameof(Postgre_Create_Delete_Test));
            //TestHelper.OpenDistributorHostForDb(CreateUniqueServerId(), new ConnectionConfiguration("testService", 10));

            writer.Start();

            var data = new StoredData(1);
            var createRequest = CreateRequest(data);
            var result = writer.Input.ProcessSync(createRequest);
            Assert.IsFalse(result.IsError);

            var deleteRequest = DeleteRequest(data.Id);
            var deleteResult = writer.Input.ProcessSync(deleteRequest);
            Assert.IsFalse(deleteResult.IsError);

            var readRequest = ReadRequest(data.Id);
            var resultRead = writer.Input.ReadOperation(readRequest);
            Assert.IsFalse(resultRead.Transaction.IsError);
            Assert.IsNull(resultRead.Data);

            var deleteRequest2 = DeleteRequest(data.Id);
            var deleteResult2 = writer.Db.DeleteFull(deleteRequest);
            Assert.IsFalse(deleteResult2.IsError);

            var readRequest2 = ReadRequest(data.Id);
            var resultRead2 = writer.Input.ReadOperation(readRequest);
            Assert.IsFalse(resultRead2.Transaction.IsError);
            Assert.IsNull(resultRead2.Data);

            writer.Dispose();
        }


        [TestMethod]
        public void Postgre_CRUD_Multiple_Test()
        {
            CreateHashFileForSingleWriter(nameof(Postgre_CRUD_Multiple_Test));
            var writer = CreatePostgreWriter(nameof(Postgre_CRUD_Multiple_Test));
            TestProxy.TestNetDistributorForProxy distrib;
            using (TestHelper.OpenDistributorHostForDb(CreateUniqueServerId(), new ConnectionConfiguration("testService", 10), out distrib))
            {
                writer.Start();

                for (int i = 1; i < 100; i++)
                {
                    var data = new StoredData(i);
                    var createRequest = CreateRequest(data);
                    var result = writer.Input.ProcessSync(createRequest);
                    Assert.IsFalse(result.IsError);
                }

                for (int i = 99; i >= 1; i--)
                {
                    var readRequest = ReadRequest(i);
                    var resultRead = writer.Input.ReadOperation(readRequest);
                    Assert.IsFalse(resultRead.Transaction.IsError);
                    Assert.IsNotNull(resultRead.Data);
                    Assert.AreEqual(i, CommonDataSerializer.Deserialize<StoredData>(resultRead.Data).Id);
                }

                for (int i = 1; i < 100; i++)
                {
                    var updateData = new StoredData(i);
                    var updateRequest = UpdateRequest(updateData);
                    var updateResult = writer.Input.ProcessSync(updateRequest);
                    Assert.IsFalse(updateResult.IsError);
                }

                for (int i = 1; i < 100; i += 2)
                {
                    var deleteRequest = DeleteRequest(i);
                    var deleteResult = writer.Input.ProcessSync(deleteRequest);
                    Assert.IsFalse(deleteResult.IsError);
                }

                for (int i = 1; i < 100; i++)
                {
                    var readRequest = ReadRequest(i);
                    var resultRead = writer.Input.ReadOperation(readRequest);
                    Assert.IsFalse(resultRead.Transaction.IsError);
                    if ((i % 2) == 1)
                        Assert.IsNull(resultRead.Data);
                    else
                        Assert.IsNotNull(resultRead.Data);
                }

                writer.Dispose();
            }
        }


        [TestMethod]
        public void Postgre_Restore_Stuff_Test()
        {
            CreateHashFileForTwoWriters(nameof(Postgre_Restore_Stuff_Test));
            //var writer1 = CreatePostgreWriter(nameof(Postgre_Restore_Stuff_Test), 0);
            var writer2 = CreatePostgreWriter(nameof(Postgre_Restore_Stuff_Test), 1);
            TestProxy.TestNetDistributorForProxy distrib;
            using (TestHelper.OpenDistributorHostForDb(CreateUniqueServerId(), new ConnectionConfiguration("testService", 10), out distrib))
            {
                writer2.Start();

                for (int i = 1; i < 100; i++)
                {
                    var data = new StoredData(i);
                    var createRequest = CreateRequest(data);
                    var result = writer2.Input.ProcessSync(createRequest);
                    Assert.IsFalse(result.IsError);
                }

                List<int> idsToRestore = new List<int>();
                using (var connection = new Npgsql.NpgsqlConnection(ConnectionString))
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "SELECT Meta_Id FROM metatable_teststored WHERE meta_local = 1";
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                                idsToRestore.Add(reader.GetInt32(0));
                        }
                    }
                }

                List<InnerData> readedForRestore = new List<InnerData>(); 
                Action<List<InnerData>> procesor = data =>
                    {
                        lock (readedForRestore)
                            readedForRestore.AddRange(data);
                    };

                var restoreResult = writer2.Db.GetDbModules[1].AsyncProcess(
                    new Impl.Writer.Db.RestoreDataContainer(false, false, 100, procesor, meta => true, true));

                Assert.IsTrue(!restoreResult.IsError || restoreResult.Description == "");
                Assert.AreEqual(idsToRestore.Count, readedForRestore.Count);
                Assert.IsTrue(readedForRestore.Select(o => CommonDataSerializer.Deserialize<int>(o.Key)).All(o => idsToRestore.Contains(o)));

                writer2.Dispose();
            }
        }
    }
}

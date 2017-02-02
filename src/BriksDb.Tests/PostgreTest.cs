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
using Qoollo.Tests.TestCollector;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Tests.TestWriter;
using Qoollo.Impl.Collector.Merge;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Collector.Model;
using Qoollo.Impl.Collector.Distributor;
using Qoollo.Impl.Collector.Background;
using Qoollo.Impl.Collector;
using Qoollo.Impl.Postgre.Internal;

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
        public void Postgre_Restore_Single_Test()
        {
            CreateHashFileForTwoWriters(nameof(Postgre_Restore_Single_Test));
            //var writer1 = CreatePostgreWriter(nameof(Postgre_Restore_Single_Test), 0);
            var writer2 = CreatePostgreWriter(nameof(Postgre_Restore_Single_Test), 1);
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
                Action<InnerData> procesor = data =>
                {
                    lock (readedForRestore)
                        readedForRestore.Add(data);
                };

                var restoreResult = writer2.Db.GetDbModules[1].AsyncProcess(
                    new Impl.Writer.Db.RestoreDataContainer(false, false, 100, procesor, meta => true, usePackage: false));

                Assert.IsTrue(!restoreResult.IsError || restoreResult.Description == "");
                Assert.AreEqual(idsToRestore.Count, readedForRestore.Count);
                Assert.IsTrue(readedForRestore.Select(o => CommonDataSerializer.Deserialize<int>(o.Key)).All(o => idsToRestore.Contains(o)));

                writer2.Dispose();
            }
        }

        [TestMethod]
        public void Postgre_Restore_Package_Test()
        {
            CreateHashFileForTwoWriters(nameof(Postgre_Restore_Package_Test));
            //var writer1 = CreatePostgreWriter(nameof(Postgre_Restore_Package_Test), 0);
            var writer2 = CreatePostgreWriter(nameof(Postgre_Restore_Package_Test), 1);
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
                    new Impl.Writer.Db.RestoreDataContainer(false, false, 100, procesor, meta => true, usePackage: true));

                Assert.IsTrue(!restoreResult.IsError || restoreResult.Description == "");
                Assert.AreEqual(idsToRestore.Count, readedForRestore.Count);
                Assert.IsTrue(readedForRestore.Select(o => CommonDataSerializer.Deserialize<int>(o.Key)).All(o => idsToRestore.Contains(o)));

                writer2.Dispose();
            }
        }



        [TestMethod]
        public void Postgre_Lexer_Test()
        {
            var parseRes = Impl.Postgre.Internal.ScriptParsing.TokenizedScript.Parse(
                "SELECT Id FROM A a JOIN b b ON a.Id=b.Id WHERE Id > 10 AND Id > '1''1' ORDER  BY Id DESC");

            Assert.IsNotNull(parseRes);
            Assert.AreEqual(23, parseRes.Tokens.Count);
        }

        [TestMethod]
        public void Postgre_Parser_Test()
        {
            var parseRes = Impl.Postgre.Internal.ScriptParsing.PostgreSelectScript.Parse(
                @"DECLARE stuff;
                    WITH Ololo AS (SELECT * FROM Test)
                    SELECT *, Id AS ""Id"", Ololo, 1 + 2 AS Calc
                    FROM A a JOIN b b ON a.Id=b.Id 
                    WHERE Id > 10 AND Id > '1''1' 
                    ORDER  BY Id DESC
                    LIMIT 10
                    OFFSET 10;");

            Assert.IsNotNull(parseRes);
            Assert.AreEqual("DECLARE stuff;", parseRes.PreSelectPart.ToString());
            Assert.IsNotNull(parseRes.With);
            Assert.AreEqual("WITH Ololo AS (SELECT * FROM Test)", parseRes.With.ToString());
            Assert.IsNotNull(parseRes.Select);
            Assert.AreEqual(@"SELECT *, Id AS ""Id"", Ololo, 1 + 2 AS Calc", parseRes.Select.ToString());
            Assert.AreEqual(4, parseRes.Select.Keys.Count);
            Assert.AreEqual("Id", parseRes.Select.Keys[1].GetKeyName());
            Assert.IsNotNull(parseRes.From);
            Assert.AreEqual("FROM A a JOIN b b ON a.Id=b.Id", parseRes.From.ToString());
            Assert.IsNotNull(parseRes.Where);
            Assert.AreEqual("WHERE Id > 10 AND Id > '1''1'", parseRes.Where.ToString());
            Assert.IsNotNull(parseRes.OrderBy);
            Assert.AreEqual("ORDER  BY Id DESC", parseRes.OrderBy.ToString());
            Assert.AreEqual(1, parseRes.OrderBy.Keys.Count);
            Assert.AreEqual("id", parseRes.OrderBy.Keys[0].GetKeyName());
            Assert.AreEqual(OrderType.Desc, parseRes.OrderBy.Keys[0].OrderType);
            Assert.IsNotNull(parseRes.Limit);
            Assert.AreEqual("LIMIT 10", parseRes.Limit.ToString());
            Assert.IsNotNull(parseRes.Offset);
            Assert.AreEqual("OFFSET 10", parseRes.Offset.ToString());
            Assert.AreEqual(";", parseRes.PostSelectPart.ToString());

            var fromatted = parseRes.Format();
            Assert.IsNotNull(fromatted);


            var parseRes2 = Impl.Postgre.Internal.ScriptParsing.PostgreSelectScript.Parse(
                @"  SELECT (1 + 2) AS ""Field"", (public.""Table"".""Id""), Table.Id, 1 AS One
                    From ""Table""");

            Assert.AreEqual(4, parseRes2.Select.Keys.Count);
            Assert.IsFalse(parseRes2.Select.Keys[1].IsCalculatable);
            Assert.AreEqual("Id", parseRes2.Select.Keys[1].GetKeyName());
            Assert.AreEqual("id", parseRes2.Select.Keys[2].GetKeyName());
            Assert.AreEqual("one", parseRes2.Select.Keys[3].GetKeyName());
            Assert.IsTrue(parseRes2.Select.Keys[3].IsCalculatable);
        }



        [TestMethod]
        public void Postgre_SelectQuery_Test()
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


                var selectDesc = new Impl.Collector.Parser.SelectDescription(
                    new Impl.Collector.Parser.FieldDescription("id", typeof(int))
                    {
                        Value = 1000
                    },
                    $"SELECT id FROM {TableName} ORDER BY Id DESC",
                    200,
                    new List<Impl.Collector.Parser.FieldDescription>())
                {
                    TableName = TableName
                };


                var selectResult = writer.Input.SelectQuery(selectDesc);
                Assert.IsNotNull(selectResult);
                Assert.IsFalse(selectResult.Item1.IsError);
                Assert.AreEqual(99, selectResult.Item2.Data.Count);

                writer.Dispose();
            }
        }


        [TestMethod]
        public void Postgre_SelectQuery_Limit_Offset_Test()
        {
            CreateHashFileForSingleWriter(nameof(Postgre_CRUD_Multiple_Test));
            var writer = CreatePostgreWriter(nameof(Postgre_CRUD_Multiple_Test));
            TestProxy.TestNetDistributorForProxy distrib;
            using (TestHelper.OpenDistributorHostForDb(CreateUniqueServerId(), new ConnectionConfiguration("testService", 10), out distrib))
            {
                writer.Start();

                for (int i = 1; i < 100; i += 2)
                {
                    var data = new StoredData(i);
                    var createRequest = CreateRequest(data);
                    var result = writer.Input.ProcessSync(createRequest);
                    Assert.IsFalse(result.IsError);
                }
                for (int i = 2; i < 100; i += 2)
                {
                    var data = new StoredData(i);
                    var createRequest = CreateRequest(data);
                    var result = writer.Input.ProcessSync(createRequest);
                    Assert.IsFalse(result.IsError);
                }


                var selectDesc = new Impl.Collector.Parser.SelectDescription(
                    new Impl.Collector.Parser.FieldDescription("id", typeof(int))
                    {
                        Value = 1000
                    },
                    $"SELECT id FROM {TableName} ORDER BY Id DESC LIMIT 10 OFFSET 10",
                    200,
                    new List<Impl.Collector.Parser.FieldDescription>())
                {
                    TableName = TableName
                };


                var selectResult = writer.Input.SelectQuery(selectDesc);
                Assert.IsNotNull(selectResult);
                Assert.IsFalse(selectResult.Item1.IsError);
                Assert.AreEqual(10, selectResult.Item2.Data.Count);
                Assert.AreEqual(89, (int)selectResult.Item2.Data[0].Key);
                Assert.AreEqual(80, (int)selectResult.Item2.Data[selectResult.Item2.Data.Count - 1].Key);

                writer.Dispose();
            }
        }


        [TestMethod]
        public void Postgre_Collector_Test()
        {
            var server1 = new ServerId("", 1);
            var server2 = new ServerId("", 2);
            var server3 = new ServerId("", 3);
            const int pageSize = 5;
            var writer = new HashWriter(new HashMapConfiguration("TestCollector", HashMapCreationMode.CreateNew, 3, 3, HashFileType.Writer));
            writer.CreateMap();
            writer.SetServer(0, server1.RemoteHost, server1.Port, 157);
            writer.SetServer(1, server2.RemoteHost, server2.Port, 157);
            writer.SetServer(2, server3.RemoteHost, server3.Port, 157);
            writer.Save();

            var loader = new TestDataLoader(pageSize);
            var parser = new PostgreScriptParser();
            parser.SetCommandsHandler(
                new UserCommandsHandler<TestCommand, Type, TestCommand, int, int, TestDbReader>(
                    new TestUserCommandCreator(), new TestMetaDataCommandCreator()));

            var merge = new OrderMerge(loader, parser, null);
            var async = new AsyncTaskModule(new QueueConfiguration(4, 10));

            var distributor =
                new DistributorModule(new CollectorModel(new DistributorHashConfiguration(1),
                    new HashMapConfiguration("TestCollector", HashMapCreationMode.ReadFromFile, 1, 1,
                        HashFileType.Writer)), async, new AsyncTasksConfiguration(TimeSpan.FromMinutes(1)));
            var back = new BackgroundModule(new QueueConfiguration(5, 10));

            var searchModule = new SearchTaskModule("Test", merge, loader, distributor, back, parser);

            #region hell

            loader.Data.Add(server1, new List<SearchData>
            {
                TestHelper.CreateData(1),
                TestHelper.CreateData(2),
                TestHelper.CreateData(4),
                TestHelper.CreateData(5),
                TestHelper.CreateData(6),
                TestHelper.CreateData(7),
                TestHelper.CreateData(8),
            });

            loader.Data.Add(server2, new List<SearchData>
            {
                TestHelper.CreateData(4),
                TestHelper.CreateData(5),
                TestHelper.CreateData(6),
                TestHelper.CreateData(7),
                TestHelper.CreateData(8),
                TestHelper.CreateData(9),
                TestHelper.CreateData(10),
                TestHelper.CreateData(11),
            });

            loader.Data.Add(server3, new List<SearchData>
            {
                TestHelper.CreateData(2),
                TestHelper.CreateData(3),
                TestHelper.CreateData(5),
                TestHelper.CreateData(7),
                TestHelper.CreateData(8),
                TestHelper.CreateData(9),
                TestHelper.CreateData(10),
                TestHelper.CreateData(11),
                TestHelper.CreateData(12),
                TestHelper.CreateData(13),
            });

            #endregion

            async.Start();
            searchModule.Start();
            distributor.Start();
            merge.Start();
            back.Start();

            var reader = searchModule.CreateReader($"SELECT Id FROM {TableName} ORDER BY Id asc");
            reader.Start();

            const int count = 13;
            for (int i = 0; i < count; i++)
            {
                Assert.IsTrue(reader.IsCanRead);

                reader.ReadNext();

                Assert.AreEqual(i + 1, reader.GetValue(0));
            }
            reader.ReadNext();
            Assert.IsFalse(reader.IsCanRead);

            reader.Dispose();

            async.Dispose();
            back.Dispose();
        }
    }
}

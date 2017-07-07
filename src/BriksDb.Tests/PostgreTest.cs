using System;
using System.Collections.Generic;
using System.Threading;
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
using Qoollo.Impl.Collector;
using Qoollo.Impl.Postgre.Internal;
using Xunit;

namespace Qoollo.Tests
{
    internal class PostgreTest:IDisposable
    {
        private const string TableName = "TestStored";
        private const string ConnectionString = "Server=127.0.0.1;" +
                                                "Port=5432;" +
                                                "Database=postgres;" +
                                                "User Id=postgres; Password = 123";

        private static StoredDataDataProvider _storedDataProvider = new StoredDataDataProvider();


        public PostgreTest()
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
            Dispose();
        }

        public void Dispose()
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

        #region support

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

        #endregion

        [Fact]
        public void Postgre_Create_Read_Test()
        {
            CreateHashFileForSingleWriter(nameof(Postgre_Create_Read_Test));
            var writer = CreatePostgreWriter(nameof(Postgre_Create_Read_Test));
            //TestHelper.OpenDistributorHostForDb(CreateUniqueServerId(), new ConnectionConfiguration("testService", 10));

            writer.Start();

            var data = new StoredData(1);
            var createRequest = CreateRequest(data);
            var result = writer.Input.ProcessSync(createRequest);
            Assert.False(result.IsError);


            var readRequest = ReadRequest(data.Id);
            var resultRead = writer.Input.ReadOperation(readRequest);
            Assert.False(resultRead.Transaction.IsError);
            Assert.NotNull(resultRead.Data);
            Assert.Equal(data.Id, CommonDataSerializer.Deserialize<StoredData>(resultRead.Data).Id);

            writer.Dispose();
        }

        [Fact]
        public void Postgre_Create_Update_Read_Test()
        {
            CreateHashFileForSingleWriter(nameof(Postgre_Create_Update_Read_Test));
            var writer = CreatePostgreWriter(nameof(Postgre_Create_Update_Read_Test));
            //TestHelper.OpenDistributorHostForDb(CreateUniqueServerId(), new ConnectionConfiguration("testService", 10));

            writer.Start();

            var data = new StoredData(1);
            var createRequest = CreateRequest(data);
            var result = writer.Input.ProcessSync(createRequest);
            Assert.False(result.IsError);

            var updateData = new StoredData(1);
            var updateRequest = UpdateRequest(updateData);
            var updateResult = writer.Input.ProcessSync(updateRequest);
            Assert.False(updateResult.IsError);

            var readRequest = ReadRequest(data.Id);
            var resultRead = writer.Input.ReadOperation(readRequest);
            Assert.False(resultRead.Transaction.IsError);
            Assert.NotNull(resultRead.Data);
            Assert.Equal(data.Id, CommonDataSerializer.Deserialize<StoredData>(resultRead.Data).Id);

            writer.Dispose();
        }

        [Fact]
        public void Postgre_Create_Delete_Test()
        {
            CreateHashFileForSingleWriter(nameof(Postgre_Create_Delete_Test));
            var writer = CreatePostgreWriter(nameof(Postgre_Create_Delete_Test));
            //TestHelper.OpenDistributorHostForDb(CreateUniqueServerId(), new ConnectionConfiguration("testService", 10));

            writer.Start();

            var data = new StoredData(1);
            var createRequest = CreateRequest(data);
            var result = writer.Input.ProcessSync(createRequest);
            Assert.False(result.IsError);

            var deleteRequest = DeleteRequest(data.Id);
            var deleteResult = writer.Input.ProcessSync(deleteRequest);
            Assert.False(deleteResult.IsError);

            var readRequest = ReadRequest(data.Id);
            var resultRead = writer.Input.ReadOperation(readRequest);
            Assert.False(resultRead.Transaction.IsError);
            Assert.Null(resultRead.Data);

            var deleteRequest2 = DeleteRequest(data.Id);
            var deleteResult2 = writer.Db.DeleteFull(deleteRequest);
            Assert.False(deleteResult2.IsError);

            var readRequest2 = ReadRequest(data.Id);
            var resultRead2 = writer.Input.ReadOperation(readRequest);
            Assert.False(resultRead2.Transaction.IsError);
            Assert.Null(resultRead2.Data);

            writer.Dispose();
        }

        [Fact]
        public void Postgre_CRUD_Multiple_Test()
        {
            CreateHashFileForSingleWriter(nameof(Postgre_CRUD_Multiple_Test));
            var writer = CreatePostgreWriter(nameof(Postgre_CRUD_Multiple_Test));
            TestHelper.OpenDistributorHostForDb(null, CreateUniqueServerId(),
                new ConnectionConfiguration("testService", 10));
            writer.Start();

            for (int i = 1; i < 100; i++)
            {
                var data = new StoredData(i);
                var createRequest = CreateRequest(data);
                var result = writer.Input.ProcessSync(createRequest);
                Assert.False(result.IsError);
            }

            for (int i = 99; i >= 1; i--)
            {
                var readRequest = ReadRequest(i);
                var resultRead = writer.Input.ReadOperation(readRequest);
                Assert.False(resultRead.Transaction.IsError);
                Assert.NotNull(resultRead.Data);
                Assert.Equal(i, CommonDataSerializer.Deserialize<StoredData>(resultRead.Data).Id);
            }

            for (int i = 1; i < 100; i++)
            {
                var updateData = new StoredData(i);
                var updateRequest = UpdateRequest(updateData);
                var updateResult = writer.Input.ProcessSync(updateRequest);
                Assert.False(updateResult.IsError);
            }

            for (int i = 1; i < 100; i += 2)
            {
                var deleteRequest = DeleteRequest(i);
                var deleteResult = writer.Input.ProcessSync(deleteRequest);
                Assert.False(deleteResult.IsError);
            }

            for (int i = 1; i < 100; i++)
            {
                var readRequest = ReadRequest(i);
                var resultRead = writer.Input.ReadOperation(readRequest);
                Assert.False(resultRead.Transaction.IsError);
                if ((i%2) == 1)
                    Assert.Null(resultRead.Data);
                else
                    Assert.NotNull(resultRead.Data);
            }

            writer.Dispose();
        }


        [Fact]
        public void Postgre_Restore_Single_Test()
        {
            CreateHashFileForTwoWriters(nameof(Postgre_Restore_Single_Test));
            //var writer1 = CreatePostgreWriter(nameof(Postgre_Restore_Single_Test), 0);
            var writer2 = CreatePostgreWriter(nameof(Postgre_Restore_Single_Test), 1);
            TestHelper.OpenDistributorHostForDb(null, CreateUniqueServerId(), new ConnectionConfiguration("testService", 10));
            writer2.Start();

            for (int i = 1; i < 100; i++)
            {
                var data = new StoredData(i);
                var createRequest = CreateRequest(data);
                var result = writer2.Input.ProcessSync(createRequest);
                Assert.False(result.IsError);
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

            Assert.True(!restoreResult.IsError || restoreResult.Description == "");
            Assert.Equal(idsToRestore.Count, readedForRestore.Count);
            Assert.True(
                readedForRestore.Select(o => CommonDataSerializer.Deserialize<int>(o.Key))
                    .All(o => idsToRestore.Contains(o)));

            writer2.Dispose();
        }

        [Fact]
        public void Postgre_Restore_Package_Test()
        {
            CreateHashFileForTwoWriters(nameof(Postgre_Restore_Package_Test));
            //var writer1 = CreatePostgreWriter(nameof(Postgre_Restore_Package_Test), 0);
            var writer2 = CreatePostgreWriter(nameof(Postgre_Restore_Package_Test), 1);
            TestHelper.OpenDistributorHostForDb(null, CreateUniqueServerId(), new ConnectionConfiguration("testService", 10));
            writer2.Start();

            for (int i = 1; i < 100; i++)
            {
                var data = new StoredData(i);
                var createRequest = CreateRequest(data);
                var result = writer2.Input.ProcessSync(createRequest);
                Assert.False(result.IsError);
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

            Assert.True(!restoreResult.IsError || restoreResult.Description == "");
            Assert.Equal(idsToRestore.Count, readedForRestore.Count);
            Assert.True(
                readedForRestore.Select(o => CommonDataSerializer.Deserialize<int>(o.Key))
                    .All(o => idsToRestore.Contains(o)));

            writer2.Dispose();
        }

        [Fact]
        public void Postgre_Lexer_Test()
        {
            var parseRes = Impl.Postgre.Internal.ScriptParsing.TokenizedScript.Parse(
                "SELECT Id FROM A a JOIN b b ON a.Id=b.Id WHERE Id > 10 AND Id > '1''1' ORDER  BY Id DESC");

            Assert.NotNull(parseRes);
            Assert.Equal(23, parseRes.Tokens.Count);
        }

        [Fact]
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

            Assert.NotNull(parseRes);
            Assert.Equal("DECLARE stuff;", parseRes.PreSelectPart.ToString());
            Assert.NotNull(parseRes.With);
            Assert.Equal("WITH Ololo AS (SELECT * FROM Test)", parseRes.With.ToString());
            Assert.NotNull(parseRes.Select);
            Assert.Equal(@"SELECT *, Id AS ""Id"", Ololo, 1 + 2 AS Calc", parseRes.Select.ToString());
            Assert.Equal(4, parseRes.Select.Keys.Count);
            Assert.Equal("Id", parseRes.Select.Keys[1].GetKeyName());
            Assert.NotNull(parseRes.From);
            Assert.Equal("FROM A a JOIN b b ON a.Id=b.Id", parseRes.From.ToString());
            Assert.NotNull(parseRes.Where);
            Assert.Equal("WHERE Id > 10 AND Id > '1''1'", parseRes.Where.ToString());
            Assert.NotNull(parseRes.OrderBy);
            Assert.Equal("ORDER  BY Id DESC", parseRes.OrderBy.ToString());
            Assert.Equal(1, parseRes.OrderBy.Keys.Count);
            Assert.Equal("id", parseRes.OrderBy.Keys[0].GetKeyName());
            Assert.Equal(OrderType.Desc, parseRes.OrderBy.Keys[0].OrderType);
            Assert.NotNull(parseRes.Limit);
            Assert.Equal("LIMIT 10", parseRes.Limit.ToString());
            Assert.NotNull(parseRes.Offset);
            Assert.Equal("OFFSET 10", parseRes.Offset.ToString());
            Assert.Equal(";", parseRes.PostSelectPart.ToString());

            var fromatted = parseRes.Format();
            Assert.NotNull(fromatted);


            var parseRes2 = Impl.Postgre.Internal.ScriptParsing.PostgreSelectScript.Parse(
                @"  SELECT (1 + 2) AS ""Field"", (public.""Table"".""Id""), Table.Id, 1 AS One
                    From ""Table""");

            Assert.Equal(4, parseRes2.Select.Keys.Count);
            Assert.False(parseRes2.Select.Keys[1].IsCalculatable);
            Assert.Equal("Id", parseRes2.Select.Keys[1].GetKeyName());
            Assert.Equal("id", parseRes2.Select.Keys[2].GetKeyName());
            Assert.Equal("one", parseRes2.Select.Keys[3].GetKeyName());
            Assert.True(parseRes2.Select.Keys[3].IsCalculatable);
        }



        [Fact]
        public void Postgre_SelectQuery_Test()
        {
            CreateHashFileForSingleWriter(nameof(Postgre_CRUD_Multiple_Test));
            var writer = CreatePostgreWriter(nameof(Postgre_CRUD_Multiple_Test));
            TestHelper.OpenDistributorHostForDb(null, CreateUniqueServerId(), new ConnectionConfiguration("testService", 10));
            writer.Start();

            for (int i = 1; i < 100; i++)
            {
                var data = new StoredData(i);
                var createRequest = CreateRequest(data);
                var result = writer.Input.ProcessSync(createRequest);
                Assert.False(result.IsError);
            }


            var selectDesc = new Impl.Collector.Parser.SelectDescription(
                new Impl.Collector.Parser.FieldDescription("id", typeof (int))
                {
                    Value = 1000
                },
                $"SELECT id FROM {TableName} ORDER BY Id DESC",
                200,
                new List<Impl.Collector.Parser.FieldDescription>())
            {
                TableName = TableName,
                OrderKeyDescriptions = new List<FieldDescription>()
                {
                    new FieldDescription("id", typeof (int)) {AsFieldName = "id"}
                }
            };


            var selectResult = writer.Input.SelectQuery(selectDesc);
            Assert.NotNull(selectResult);
            Assert.False(selectResult.Item1.IsError);
            Assert.Equal(99, selectResult.Item2.Data.Count);

            writer.Dispose();
        }


        [Fact]
        public void Postgre_SelectQuery_Limit_Offset_Test()
        {
            CreateHashFileForSingleWriter(nameof(Postgre_CRUD_Multiple_Test));
            var writer = CreatePostgreWriter(nameof(Postgre_CRUD_Multiple_Test));
            TestHelper.OpenDistributorHostForDb(null, CreateUniqueServerId(), new ConnectionConfiguration("testService", 10));
            writer.Start();

            for (int i = 1; i < 100; i += 2)
            {
                var data = new StoredData(i);
                var createRequest = CreateRequest(data);
                var result = writer.Input.ProcessSync(createRequest);
                Assert.False(result.IsError);
            }
            for (int i = 2; i < 100; i += 2)
            {
                var data = new StoredData(i);
                var createRequest = CreateRequest(data);
                var result = writer.Input.ProcessSync(createRequest);
                Assert.False(result.IsError);
            }


            var selectDesc = new Impl.Collector.Parser.SelectDescription(
                new Impl.Collector.Parser.FieldDescription("id", typeof (int))
                {
                    Value = 1000
                },
                $"SELECT id FROM {TableName} ORDER BY Id DESC LIMIT 10 OFFSET 10",
                200,
                new List<Impl.Collector.Parser.FieldDescription>())
            {
                TableName = TableName,
                OrderKeyDescriptions = new List<FieldDescription>()
                {
                    new FieldDescription("id", typeof (int)) {AsFieldName = "id"}
                }
            };


            var selectResult = writer.Input.SelectQuery(selectDesc);
            Assert.NotNull(selectResult);
            Assert.False(selectResult.Item1.IsError);
            Assert.Equal(10, selectResult.Item2.Data.Count);
            Assert.Equal(89, (int) selectResult.Item2.Data[0].Key);
            Assert.Equal(80, (int) selectResult.Item2.Data[selectResult.Item2.Data.Count - 1].Key);

            writer.Dispose();
        }


        [Fact]
        public void Postgre_SelectQuery_MultiOrder_Test()
        {
            CreateHashFileForSingleWriter(nameof(Postgre_CRUD_Multiple_Test));
            var writer = CreatePostgreWriter(nameof(Postgre_CRUD_Multiple_Test));
            TestHelper.OpenDistributorHostForDb(null, CreateUniqueServerId(), new ConnectionConfiguration("testService", 10));
            writer.Start();

            for (int i = 1; i < 100; i++)
            {
                var data = new StoredData(i);
                var createRequest = CreateRequest(data);
                var result = writer.Input.ProcessSync(createRequest);
                Assert.False(result.IsError);
            }


            var selectDesc = new Impl.Collector.Parser.SelectDescription(
                new Impl.Collector.Parser.FieldDescription("id", typeof (int))
                {
                    Value = 1000,
                    IsFirstAsk = false
                },
                $"SELECT Id, (CASE WHEN id > 10 THEN 1 ELSE 2 END) AS Test FROM {TableName} ORDER BY Test DESC, Id DESC",
                200,
                new List<Impl.Collector.Parser.FieldDescription>())
            {
                TableName = TableName,
                OrderKeyDescriptions = new List<FieldDescription>()
                {
                    new FieldDescription("test", typeof (int)) {AsFieldName = "test", Value = 10000},
                    new FieldDescription("id", typeof (int)) {AsFieldName = "id", Value = 100000}
                }
            };


            var selectResult = writer.Input.SelectQuery(selectDesc);
            Assert.NotNull(selectResult);
            Assert.False(selectResult.Item1.IsError);
            Assert.Equal(99, selectResult.Item2.Data.Count);
            Assert.Equal(10, (int) selectResult.Item2.Data[0].Key);
            Assert.Equal(99, (int) selectResult.Item2.Data[10].Key);

            writer.Dispose();
        }


        [Fact]
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

            var merge = new OrderMerge(null, parser);
            //, new CollectorModel(new DistributorHashConfiguration(1),
            //        new HashMapConfiguration("TestCollector", HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Writer)));
            var async = new AsyncTaskModule(null, new QueueConfiguration(4, 10));
            //new CollectorModel(new DistributorHashConfiguration(1),
            //        new HashMapConfiguration("TestCollector", HashMapCreationMode.ReadFromFile, 1, 1,
            //            HashFileType.Writer)), async
            var distributor = new DistributorModule(null, new AsyncTasksConfiguration(TimeSpan.FromMinutes(1)));
            var back = new BackgroundModule(null);

            var searchModule = new SearchTaskModule(null, "Test", merge, parser);

            #region hell

            loader.Data.Add(server1, new List<SearchData>
            {
                TestHelper.CreateData(1, "Id"),
                TestHelper.CreateData(2, "Id"),
                TestHelper.CreateData(4, "Id"),
                TestHelper.CreateData(5, "Id"),
                TestHelper.CreateData(6, "Id"),
                TestHelper.CreateData(7, "Id"),
                TestHelper.CreateData(8, "Id"),
            });

            loader.Data.Add(server2, new List<SearchData>
            {
                TestHelper.CreateData(4, "Id"),
                TestHelper.CreateData(5, "Id"),
                TestHelper.CreateData(6, "Id"),
                TestHelper.CreateData(7, "Id"),
                TestHelper.CreateData(8, "Id"),
                TestHelper.CreateData(9, "Id"),
                TestHelper.CreateData(10, "Id"),
                TestHelper.CreateData(11, "Id"),
            });

            loader.Data.Add(server3, new List<SearchData>
            {
                TestHelper.CreateData(2, "Id"),
                TestHelper.CreateData(3, "Id"),
                TestHelper.CreateData(5, "Id"),
                TestHelper.CreateData(7, "Id"),
                TestHelper.CreateData(8, "Id"),
                TestHelper.CreateData(9, "Id"),
                TestHelper.CreateData(10, "Id"),
                TestHelper.CreateData(11, "Id"),
                TestHelper.CreateData(12, "Id"),
                TestHelper.CreateData(13, "Id"),
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
                Assert.True(reader.IsCanRead);

                reader.ReadNext();

                Assert.Equal(i + 1, reader.GetValue(0));
            }
            reader.ReadNext();
            Assert.False(reader.IsCanRead);

            reader.Dispose();

            async.Dispose();
            back.Dispose();
        }


        [Fact]
        public void Postgre_Collector_MultipleKey_Test()
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
            //_kernel.Bind<IDataLoader>().ToConstant(loader);

            var parser = new PostgreScriptParser();
            parser.SetCommandsHandler(
                new UserCommandsHandler<TestCommand, Type, TestCommand, int, int, TestDbReader>(
                    new TestUserCommandCreator(), new TestMetaDataCommandCreator()));

            var merge = new OrderMerge(null, parser); 
                //new CollectorModel(new DistributorHashConfiguration(1), 
                //    new HashMapConfiguration("TestCollector", HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Writer)));
            var async = new AsyncTaskModule(null,new QueueConfiguration(4, 10));

            //new CollectorModel(new DistributorHashConfiguration(1),
            //        new HashMapConfiguration("TestCollector", HashMapCreationMode.ReadFromFile, 1, 1,
            //            HashFileType.Writer)), async,

            var distributor = new DistributorModule(null, new AsyncTasksConfiguration(TimeSpan.FromMinutes(1)));
            var back = new BackgroundModule(null);

            var searchModule = new SearchTaskModule(null, "Test", merge, parser);

            #region hell

            loader.Data.Add(server1, new List<SearchData>
            {
                TestHelper.CreateData2(1, 1),
                TestHelper.CreateData2(5, 1),
                TestHelper.CreateData2(7, 1),
                TestHelper.CreateData2(2, 2),
                TestHelper.CreateData2(4, 2),             
                TestHelper.CreateData2(6, 2),              
                TestHelper.CreateData2(8, 2),
            });

            loader.Data.Add(server2, new List<SearchData>
            {
                TestHelper.CreateData2(5, 1),
                TestHelper.CreateData2(7, 1),
                TestHelper.CreateData2(9, 1),
                TestHelper.CreateData2(11, 1),
                TestHelper.CreateData2(4, 2),              
                TestHelper.CreateData2(6, 2),              
                TestHelper.CreateData2(8, 2),               
                TestHelper.CreateData2(10, 2),
                
            });

            loader.Data.Add(server3, new List<SearchData>
            {
                TestHelper.CreateData2(3, 1),
                TestHelper.CreateData2(5, 1),
                TestHelper.CreateData2(7, 1),
                TestHelper.CreateData2(9, 1),
                TestHelper.CreateData2(11, 1),
                TestHelper.CreateData2(13, 1),
                TestHelper.CreateData2(2, 2),             
                TestHelper.CreateData2(8, 2),          
                TestHelper.CreateData2(10, 2),               
                TestHelper.CreateData2(12, 2),           
            });

            List<int> expectedOrder = new List<int>()
            {
                1, 3, 5, 7, 9, 11, 13, 2, 4, 6, 8, 10, 12
            };

            #endregion

            async.Start();
            searchModule.Start();
            distributor.Start();
            merge.Start();
            back.Start();

            var reader = searchModule.CreateReader($"SELECT Id, (2 - (Id % 2)) AS valCount FROM {TableName} ORDER BY valCount ASC, Id asc");
            reader.Start();

            const int count = 13;
            for (int i = 0; i < count; i++)
            {
                Assert.True(reader.IsCanRead);

                reader.ReadNext();

                Assert.Equal(expectedOrder[i], reader.GetValue(0));
                Assert.Equal((long)(2 - (expectedOrder[i] % 2)), reader.GetValue(1));
            }
            reader.ReadNext();
            Assert.False(reader.IsCanRead);

            reader.Dispose();

            async.Dispose();
            back.Dispose();
        }
    }
}

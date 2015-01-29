using System;
using System.Data;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Client.Configuration;
using Qoollo.Client.DistributorGate;
using Qoollo.Client.Request;
using Qoollo.Client.WriterGate;
using Qoollo.Impl.Collector;
using Qoollo.Impl.Collector.Background;
using Qoollo.Impl.Collector.CollectorNet;
using Qoollo.Impl.Collector.Distributor;
using Qoollo.Impl.Collector.Load;
using Qoollo.Impl.Collector.Merge;
using Qoollo.Impl.Collector.Model;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Collector.Tasks;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Sql.Internal;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestCollector;
using Qoollo.Tests.TestModules;
using Qoollo.Tests.TestWriter;

namespace Qoollo.Tests
{
    [TestClass]
    public class TestCollectorModules
    {
        [TestMethod]
        public void SingleServerSearchTask_GetData_CheckData()
        {
            var task = new SingleServerSearchTask(new ServerId("", 0), "",
                new FieldDescription("", typeof(int)), "");
            var page = new List<SearchData>();

            const int count = 5;
            for (int i = 0; i < count * 2; i++)
            {
                page.Add(new SearchData(null, i));
            }
            task.AddPage(page);

            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(i, task.GetData().Key);
                task.IncrementPosition();
            }
            task.AddPage(page);

            for (int i = count; i < count * 2; i++)
            {
                Assert.AreEqual(i, task.GetData().Key);
                task.IncrementPosition();
            }

            for (int i = count * 2; i < count * 4; i++)
            {
                Assert.AreEqual(i - count * 2, task.GetData().Key);
                task.IncrementPosition();
            }
        }

        [TestMethod]
        public void CollectorModel_GetSystemState_CheckWritersState()
        {
            const int countReplics = 2;
            var writer = new HashWriter(new HashMapConfiguration("TestCollectorModel", HashMapCreationMode.CreateNew, 4, 3, HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", 1, 157);
            writer.SetServer(1, "localhost", 2, 157);
            writer.SetServer(2, "localhost", 3, 157);
            writer.SetServer(3, "localhost", 4, 157);
            writer.Save();

            var model = new CollectorModel(new DistributorHashConfiguration(countReplics),
                new HashMapConfiguration("TestCollectorModel", HashMapCreationMode.ReadFromFile, 1, countReplics, HashFileType.Writer));
            model.Start();


            var state = model.GetSystemState();
            Assert.AreEqual(SystemSearchStateInner.AllServersAvailable, state);

            model.ServerNotAvailable(new ServerId("localhost", 1));
            state = model.GetSystemState();
            Assert.AreEqual(SystemSearchStateInner.AllDataAvailable, state);

            model.ServerNotAvailable(new ServerId("localhost", 3));
            state = model.GetSystemState();
            Assert.AreEqual(SystemSearchStateInner.AllDataAvailable, state);

            model.ServerNotAvailable(new ServerId("localhost", 2));
            state = model.GetSystemState();
            Assert.AreEqual(SystemSearchStateInner.SomeDataUnavailable, state);

            model.ServerAvailable(new ServerId("localhost", 1));
            state = model.GetSystemState();
            Assert.AreEqual(SystemSearchStateInner.SomeDataUnavailable, state);

            model.ServerNotAvailable(new ServerId("localhost", 4));
            state = model.GetSystemState();
            Assert.AreEqual(SystemSearchStateInner.SomeDataUnavailable, state);

            model.ServerAvailable(new ServerId("localhost", 3));
            state = model.GetSystemState();
            Assert.AreEqual(SystemSearchStateInner.AllDataAvailable, state);
        }

        [TestMethod]
        public void BackgroundModule_CountLoads_CountLoads()
        {
            var background = new BackgroundModule(new QueueConfiguration(1, 100));
            background.Start();

            var search = new TestSelectTask("", new List<ServerId>(), "", new FieldDescription("", typeof(int)));

            background.Run(search,
                () => search.BackgroundLoadInner(() => SystemSearchStateInner.AllDataAvailable, null, null));


            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            Assert.AreEqual(1, search.CountLoads);
            search.GetDataInner();

            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            Assert.AreEqual(2, search.CountLoads);
            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            Assert.AreEqual(2, search.CountLoads);
            search.Finish = true;
            search.GetDataInner();

            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            Assert.AreEqual(3, search.CountLoads);
            search.GetDataInner();

            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            Assert.AreEqual(3, search.CountLoads);

            background.Dispose();
        }

        [TestMethod]
        public void OrderMerge_GetMergeFunction_CheckData()
        {
            const int pageSize = 5;

            var loader = new TestDataLoader(pageSize);

            var merge = new OrderMerge(loader, new TestIntParser());
            var server1 = new ServerId("", 1);
            var server2 = new ServerId("", 2);
            var server3 = new ServerId("", 3);

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

            var task = new OrderSelectTask(new List<ServerId> { server1, server2, server3 },
                new FieldDescription("", typeof(int)), new FieldDescription("", typeof(int)), "asc", -1, 5,
                new List<FieldDescription>(), "");
            var function = merge.GetMergeFunction(ScriptType.OrderAsc);

            var result = function(task, task.SearchTasks);
            Assert.AreEqual(1, result[0].Key);
            Assert.AreEqual(2, result[1].Key);
            Assert.AreEqual(3, result[2].Key);
            Assert.AreEqual(4, result[3].Key);
            Assert.AreEqual(5, result[4].Key);
            result = function(task, task.SearchTasks);
            Assert.AreEqual(6, result[0].Key);
            Assert.AreEqual(7, result[1].Key);
            Assert.AreEqual(8, result[2].Key);
            Assert.AreEqual(9, result[3].Key);
            Assert.AreEqual(10, result[4].Key);
            Assert.IsTrue(task.SearchTasks[0].IsAllDataRead);
            result = function(task, task.SearchTasks);
            Assert.AreEqual(11, result[0].Key);
            Assert.AreEqual(12, result[1].Key);
            Assert.AreEqual(13, result[2].Key);
            Assert.IsTrue(task.SearchTasks[0].IsAllDataRead);
            Assert.IsTrue(task.SearchTasks[1].IsAllDataRead);
            Assert.IsTrue(task.SearchTasks[2].IsAllDataRead);
            result = function(task, task.SearchTasks);
            Assert.IsTrue(result.Count == 0);
            Assert.IsTrue(task.SearchTasks[0].IsAllDataRead);
            Assert.IsTrue(task.SearchTasks[1].IsAllDataRead);
            Assert.IsTrue(task.SearchTasks[2].IsAllDataRead);
        }

        [TestMethod]
        public void SearchTaskModule_CreateReader_ReadData()
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
            var parser = new TestIntParser();
            parser.SetCommandsHandler(
                new UserCommandsHandler<TestCommand, Type, TestCommand, int, int, TestDbReader>(
                    new TestUserCommandCreator(), new TestMetaDataCommandCreator()));

            var merge = new OrderMerge(loader, parser);
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

            var reader = searchModule.CreateReader("asc");
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

        [TestMethod]
        public void SearchTaskModule_CreateReader_LimitDataRead()
        {
            var server1 = new ServerId("", 1);
            var server2 = new ServerId("", 2);
            var server3 = new ServerId("", 3);
            const int pageSize = 5;
            var writer = new HashWriter(new HashMapConfiguration("TestCollector2", HashMapCreationMode.CreateNew, 3, 3, HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, server1.RemoteHost, server1.Port, 157);
            writer.SetServer(1, server2.RemoteHost, server2.Port, 157);
            writer.SetServer(2, server3.RemoteHost, server3.Port, 157);
            writer.Save();

            var loader = new TestDataLoader(pageSize);
            var parser = new TestIntParser();
            parser.SetCommandsHandler(
                new UserCommandsHandler<TestCommand, Type, TestCommand, int, int, TestDbReader>(
                    new TestUserCommandCreator(), new TestMetaDataCommandCreator()));
            var merge = new OrderMerge(loader, parser);
            var async = new AsyncTaskModule(new QueueConfiguration(4, 10));
            var distributor =
                new DistributorModule(new CollectorModel(new DistributorHashConfiguration(1),
                    new HashMapConfiguration("TestCollector2", HashMapCreationMode.ReadFromFile, 1, 1,
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

            searchModule.Start();
            distributor.Start();
            merge.Start();
            back.Start();
            async.Start();

            var reader = searchModule.CreateReader("asc", 10);
            reader.Start();

            const int count = 10;
            for (int i = 0; i < count; i++)
            {
                Assert.IsTrue(reader.IsCanRead);

                reader.ReadNext();

                Assert.AreEqual(i + 1, reader.GetValue(0));
            }
            reader.ReadNext();
            Assert.IsFalse(reader.IsCanRead);

            reader.Dispose();

            back.Dispose();
            async.Dispose();
        }

        [TestMethod]
        public void SearchTaskModule_CreateReader_LimitDataReadAndUserPage()
        {
            var server1 = new ServerId("", 1);
            var server2 = new ServerId("", 2);
            var server3 = new ServerId("", 3);
            const int pageSize = 5;
            var writer = new HashWriter(new HashMapConfiguration("TestCollector3", HashMapCreationMode.CreateNew, 3, 3, HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, server1.RemoteHost, server1.Port, 157);
            writer.SetServer(1, server2.RemoteHost, server2.Port, 157);
            writer.SetServer(2, server3.RemoteHost, server3.Port, 157);
            writer.Save();

            var loader = new TestDataLoader(pageSize);
            var parser = new TestIntParser();
            parser.SetCommandsHandler(
                new UserCommandsHandler<TestCommand, Type, TestCommand, int, int, TestDbReader>(
                    new TestUserCommandCreator(), new TestMetaDataCommandCreator()));
            var merge = new OrderMerge(loader, parser);
            var async = new AsyncTaskModule(new QueueConfiguration(4, 10));
            var distributor =
                new DistributorModule(new CollectorModel(new DistributorHashConfiguration(1),
                    new HashMapConfiguration("TestCollector3", HashMapCreationMode.ReadFromFile, 1, 1,
                        HashFileType.Distributor)), async, new AsyncTasksConfiguration(TimeSpan.FromMinutes(1)));
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

            var reader = searchModule.CreateReader("asc", 10, 5);
            reader.Start();

            const int count = 10;
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

        [TestMethod]
        public void SearchTaskModule_CreateReader_UnlimitDataReadAndUserPage()
        {
            var server1 = new ServerId("", 1);
            var server2 = new ServerId("", 2);
            var server3 = new ServerId("", 3);
            const int pageSize = 5;
            var writer = new HashWriter(new HashMapConfiguration("TestCollector4", HashMapCreationMode.CreateNew, 3, 3, HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, server1.RemoteHost, server1.Port, 157);
            writer.SetServer(1, server2.RemoteHost, server2.Port, 157);
            writer.SetServer(2, server3.RemoteHost, server3.Port, 157);
            writer.Save();

            var loader = new TestDataLoader(pageSize);
            var parser = new TestIntParser();
            parser.SetCommandsHandler(
                new UserCommandsHandler<TestCommand, Type, TestCommand, int, int, TestDbReader>(
                    new TestUserCommandCreator(), new TestMetaDataCommandCreator()));
            var merge = new OrderMerge(loader, parser);
            var async = new AsyncTaskModule(new QueueConfiguration(4, 10));
            var distributor =
                new DistributorModule(new CollectorModel(new DistributorHashConfiguration(1),
                    new HashMapConfiguration("TestCollector4", HashMapCreationMode.ReadFromFile, 1, 1,
                        HashFileType.Writer)), async, new AsyncTasksConfiguration(TimeSpan.FromMinutes(1)));
            var back = new BackgroundModule(new QueueConfiguration(5, 10));

            var searchModule = new SearchTaskModule("", merge, loader, distributor, back, parser);

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

            searchModule.Start();
            distributor.Start();
            merge.Start();
            back.Start();
            async.Start();

            var reader = searchModule.CreateReader("asc", -1, 5);
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

        [TestMethod]
        public void CollectorNet_ReadFromWriter()
        {
            const int proxyServer = 22337;
            const int distrServer1 = 22338;
            const int distrServer12 = 22339;
            const int st1 = 22335;
            const int st2 = 22336;

            #region hell

            var writer = new HashWriter(new HashMapConfiguration("TestCollectorNet", HashMapCreationMode.CreateNew, 1, 1, HashFileType.Distributor));
            writer.CreateMap();
            writer.SetServer(0, "localhost", st1, st2);
            writer.Save();

            var common = new CommonConfiguration(1, 100);

            var netconfig = new NetConfiguration("localhost", proxyServer, "testService", 10);
            var toconfig = new ProxyConfiguration(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));

            var proxy = new TestGate(netconfig, toconfig, common);

            var distrNet = new DistributorNetConfiguration("localhost",
                distrServer1, distrServer12, "testService", 10);
            var distrConf = new DistributorConfiguration(1, "TestCollectorNet",
                TimeSpan.FromMilliseconds(100000), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(10000));

            var distr = new DistributorApi(distrNet, distrConf, common);

            var storageNet = new StorageNetConfiguration("localhost", st1, st2, "testService", 10);
            var storageConfig = new StorageConfiguration("TestCollectorNet", 1, 10, TimeSpan.FromHours(1), TimeSpan.FromHours(1),
                TimeSpan.FromHours(1), TimeSpan.FromHours(1), false);

            var storage = new WriterApi(storageNet, storageConfig, common);
            var async = new AsyncTaskModule(new QueueConfiguration(4, 10));
            var distributor =
                new DistributorModule(new CollectorModel(new DistributorHashConfiguration(1),
                    new HashMapConfiguration("TestCollectorNet", HashMapCreationMode.ReadFromFile, 1, 1,
                        HashFileType.Collector)), async, new AsyncTasksConfiguration(TimeSpan.FromMinutes(1)));

            var net = new CollectorNetModule(new ConnectionConfiguration("testService", 10),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout), distributor);

            distributor.SetNetModule(net);

            var back = new BackgroundModule(new QueueConfiguration(5, 10));
            var loader = new DataLoader(net, 100, back);

            var parser = new TestIntParser();
            parser.SetCommandsHandler(
                new UserCommandsHandler<TestCommand, Type, TestCommand, int, int, TestDbReader>(
                    new TestUserCommandCreator(), new TestMetaDataCommandCreator()));
            var merge = new OrderMerge(loader, parser);

            var searchModule = new SearchTaskModule("Int", merge, loader, distributor, back, parser);

            storage.Build();
            proxy.Build();
            distr.Build();

            storage.AddDbModule(new TestInMemoryDbFactory());

            storage.Start();
            proxy.Start();
            distr.Start();

            searchModule.Start();
            distributor.Start();
            merge.Start();
            back.Start();
            net.Start();
            async.Start();

            #endregion

            proxy.Int.SayIAmHere("localhost", distrServer1);

            const int count = 20;

            for (int i = 0; i < count; i++)
            {
                var request = proxy.Int.CreateSync(i, i);
                Assert.AreEqual(RequestState.Complete, request.State);
            }

            var reader = searchModule.CreateReader("asc", -1, 20);
            reader.Start();

            for (int i = 0; i < count; i++)
            {
                Assert.IsTrue(reader.IsCanRead);

                reader.ReadNext();

                Assert.AreEqual(i, reader.GetValue(0));
            }
            reader.ReadNext();
            Assert.IsFalse(reader.IsCanRead);

            reader.Dispose();
            back.Dispose();
            net.Dispose();

            storage.Dispose();
            proxy.Dispose();
            distr.Dispose();
            async.Dispose();
        }

        [TestMethod]
        public void SqlScriptParser_ParseQueryType()
        {
            var parser = new SqlScriptParser();
            var type = parser.ParseQueryType("order by");
            Assert.AreEqual(ScriptType.OrderAsc, type);
            type = parser.ParseQueryType("order by desc");
            Assert.AreEqual(ScriptType.OrderDesc, type);
            type = parser.ParseQueryType("order bsy desc");
            Assert.AreEqual(ScriptType.Unknown, type);
        }        
    }
}

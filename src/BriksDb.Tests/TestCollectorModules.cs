using System;
using System.Collections.Generic;
using System.Threading;
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
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.Sql.Internal;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestCollector;
using Qoollo.Tests.TestModules;
using Qoollo.Tests.TestWriter;
using Xunit;

namespace Qoollo.Tests
{
    [Collection("test collection 1")]
    public class TestCollectorModules: TestBase
    {
        [Fact]
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
                Assert.Equal(i, task.GetData().Key);
                task.IncrementPosition();
            }
            task.AddPage(page);

            for (int i = count; i < count * 2; i++)
            {
                Assert.Equal(i, task.GetData().Key);
                task.IncrementPosition();
            }

            for (int i = count * 2; i < count * 4; i++)
            {
                Assert.Equal(i - count * 2, task.GetData().Key);
                task.IncrementPosition();
            }
        }

        [Fact]
        public void CollectorModel_GetSystemState_CheckWritersState()
        {
            var filename = nameof(CollectorModel_GetSystemState_CheckWritersState);
            using (new FileCleaner(filename))
            {
                const int countReplics = 2;
                var writer = new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 4, 3, HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", 1, 157);
                writer.SetServer(1, "localhost", 2, 157);
                writer.SetServer(2, "localhost", 3, 157);
                writer.SetServer(3, "localhost", 4, 157);
                writer.Save();

                var model = new CollectorModel(new DistributorHashConfiguration(countReplics),
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, countReplics, HashFileType.Writer));
                model.Start();

                var state = model.GetSystemState();
                Assert.Equal(SystemSearchStateInner.AllServersAvailable, state);

                model.ServerNotAvailable(new ServerId("localhost", 1));
                state = model.GetSystemState();
                Assert.Equal(SystemSearchStateInner.AllDataAvailable, state);

                model.ServerNotAvailable(new ServerId("localhost", 3));
                state = model.GetSystemState();
                Assert.Equal(SystemSearchStateInner.AllDataAvailable, state);

                model.ServerNotAvailable(new ServerId("localhost", 2));
                state = model.GetSystemState();
                Assert.Equal(SystemSearchStateInner.SomeDataUnavailable, state);

                model.ServerAvailable(new ServerId("localhost", 1));
                state = model.GetSystemState();
                Assert.Equal(SystemSearchStateInner.SomeDataUnavailable, state);

                model.ServerNotAvailable(new ServerId("localhost", 4));
                state = model.GetSystemState();
                Assert.Equal(SystemSearchStateInner.SomeDataUnavailable, state);

                model.ServerAvailable(new ServerId("localhost", 3));
                state = model.GetSystemState();
                Assert.Equal(SystemSearchStateInner.AllDataAvailable, state);
            }
        }

        [Fact]
        public void BackgroundModule_CountLoads_CountLoads()
        {
            var background = new BackgroundModule(new QueueConfiguration(1, 100));
            background.Start();

            var search = new TestSelectTask("", new List<ServerId>(), "", new FieldDescription("", typeof(int)));

            background.Run(search,
                () => search.BackgroundLoadInner(() => SystemSearchStateInner.AllDataAvailable, null, null));


            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            Assert.Equal(1, search.CountLoads);
            search.GetDataInner();

            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            Assert.Equal(2, search.CountLoads);
            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            Assert.Equal(2, search.CountLoads);
            search.Finish = true;
            search.GetDataInner();

            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            Assert.Equal(3, search.CountLoads);
            search.GetDataInner();

            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            Assert.Equal(3, search.CountLoads);

            background.Dispose();
        }

        [Fact]
        public void OrderMerge_GetMergeFunction_CheckData()
        {
            const int pageSize = 5;

            var loader = new TestDataLoader(pageSize);

            var merge = new OrderMerge(loader, new TestIntParser(), null);
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
                new FieldDescription("Id", typeof(int)), new FieldDescription("Id", typeof(int)), "asc", -1, 5,
                new List<FieldDescription>(), "");
            var function = merge.GetMergeFunction(ScriptType.OrderAsc);

            var result = function(task, task.SearchTasks);
            Assert.Equal(1, result[0].Key);
            Assert.Equal(2, result[1].Key);
            Assert.Equal(3, result[2].Key);
            Assert.Equal(4, result[3].Key);
            Assert.Equal(5, result[4].Key);
            result = function(task, task.SearchTasks);
            Assert.Equal(6, result[0].Key);
            Assert.Equal(7, result[1].Key);
            Assert.Equal(8, result[2].Key);
            Assert.Equal(9, result[3].Key);
            Assert.Equal(10, result[4].Key);
            Assert.True(task.SearchTasks[0].IsAllDataRead);
            result = function(task, task.SearchTasks);
            Assert.Equal(11, result[0].Key);
            Assert.Equal(12, result[1].Key);
            Assert.Equal(13, result[2].Key);
            Assert.True(task.SearchTasks[0].IsAllDataRead);
            Assert.True(task.SearchTasks[1].IsAllDataRead);
            Assert.True(task.SearchTasks[2].IsAllDataRead);
            result = function(task, task.SearchTasks);
            Assert.True(result.Count == 0);
            Assert.True(task.SearchTasks[0].IsAllDataRead);
            Assert.True(task.SearchTasks[1].IsAllDataRead);
            Assert.True(task.SearchTasks[2].IsAllDataRead);

            task.Dispose();
        }

        [Fact]
        public void SearchTaskModule_CreateReader_ReadData()
        {
            var filename = nameof(SearchTaskModule_CreateReader_ReadData);
            using (new FileCleaner(filename))
            {
                var server1 = new ServerId("", 1);
                var server2 = new ServerId("", 2);
                var server3 = new ServerId("", 3);
                const int pageSize = 5;
                var writer = new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 3, 3, HashFileType.Writer));
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
                var serversModel = new CollectorModel(new DistributorHashConfiguration(1),
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1,
                        HashFileType.Writer));
                var merge = new OrderMerge(loader, parser, serversModel);
                var async = new AsyncTaskModule(new QueueConfiguration(4, 10));

                var distributor =
                    new DistributorModule(serversModel, async, new AsyncTasksConfiguration(TimeSpan.FromMinutes(1)));
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
        }

        [Fact]
        public void SearchTaskModule_CreateReader_ReadData_MultipleKeys()
        {
            var filename = nameof(SearchTaskModule_CreateReader_ReadData_MultipleKeys);
            using (new FileCleaner(filename))
            {
                var server1 = new ServerId("", 1);
                var server2 = new ServerId("", 2);
                var server3 = new ServerId("", 3);
                const int pageSize = 5;
                var writer = new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 3, 3, HashFileType.Writer));
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
                var serversModel = new CollectorModel(new DistributorHashConfiguration(1),
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1,
                        HashFileType.Writer));
                var merge = new OrderMerge(loader, parser, serversModel);
                var async = new AsyncTaskModule(new QueueConfiguration(4, 10));

                var distributor =
                    new DistributorModule(serversModel, async, new AsyncTasksConfiguration(TimeSpan.FromMinutes(1)));
                var back = new BackgroundModule(new QueueConfiguration(5, 10));

                var searchModule = new SearchTaskModule("Test", merge, loader, distributor, back, parser);

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

                var reader = searchModule.CreateReader("asc2calc");
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

        [Fact]
        public void SearchTaskModule_CreateReader_LimitDataRead()
        {
            var filename = nameof(SearchTaskModule_CreateReader_LimitDataRead);
            using (new FileCleaner(filename))
            {
                var server1 = new ServerId("", 1);
                var server2 = new ServerId("", 2);
                var server3 = new ServerId("", 3);
                const int pageSize = 5;
                var writer = new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 3, 3, HashFileType.Distributor));
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
                var serversModel = new CollectorModel(new DistributorHashConfiguration(1),
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1,
                        HashFileType.Writer));
                var merge = new OrderMerge(loader, parser, serversModel);
                var async = new AsyncTaskModule(new QueueConfiguration(4, 10));
                var distributor =
                    new DistributorModule(serversModel, async, new AsyncTasksConfiguration(TimeSpan.FromMinutes(1)));
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
                    Assert.True(reader.IsCanRead);

                    reader.ReadNext();

                    Assert.Equal(i + 1, reader.GetValue(0));
                }
                reader.ReadNext();
                Assert.False(reader.IsCanRead);

                reader.Dispose();

                back.Dispose();
                async.Dispose();
            }
        }

        [Fact]
        public void SearchTaskModule_CreateReader_LimitDataReadAndUserPage()
        {
            var filename = nameof(SearchTaskModule_CreateReader_LimitDataReadAndUserPage);
            using (new FileCleaner(filename))
            {
                var server1 = new ServerId("", 1);
                var server2 = new ServerId("", 2);
                var server3 = new ServerId("", 3);
                const int pageSize = 5;
                var writer = new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 3, 3, HashFileType.Distributor));
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
                var serversModel = new CollectorModel(new DistributorHashConfiguration(1),
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1,
                        HashFileType.Distributor));
                var merge = new OrderMerge(loader, parser, serversModel);
                var async = new AsyncTaskModule(new QueueConfiguration(4, 10));
                var distributor =
                    new DistributorModule(serversModel, async, new AsyncTasksConfiguration(TimeSpan.FromMinutes(1)));
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
        }

        [Fact]
        public void SearchTaskModule_CreateReader_UnlimitDataReadAndUserPage()
        {
            var filename = nameof(SearchTaskModule_CreateReader_UnlimitDataReadAndUserPage);
            using (new FileCleaner(filename))
            {
                var server1 = new ServerId("", 1);
                var server2 = new ServerId("", 2);
                var server3 = new ServerId("", 3);
                const int pageSize = 5;
                var writer = new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 3, 3, HashFileType.Distributor));
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
                var serversModel = new CollectorModel(new DistributorHashConfiguration(1),
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1,
                        HashFileType.Writer));
                var merge = new OrderMerge(loader, parser, serversModel);
                var async = new AsyncTaskModule(new QueueConfiguration(4, 10));
                var distributor =
                    new DistributorModule(serversModel, async, new AsyncTasksConfiguration(TimeSpan.FromMinutes(1)));
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
        }

        [Fact]
        public void CollectorNet_ReadFromWriter()
        {
            var filename = nameof(CollectorNet_ReadFromWriter);
            using (new FileCleaner(filename))
            {
                const int proxyServer = 22337;
                const int distrServer1 = 22338;
                const int distrServer12 = 22339;
                const int st1 = 22335;
                const int st2 = 22336;

                #region hell

                var writer = new HashWriter(new HashMapConfiguration(filename, HashMapCreationMode.CreateNew, 1, 1, HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", st1, st2);
                writer.Save();

                var q1 = new GlobalQueueInner();
                GlobalQueue.SetQueue(q1);

                var common = new CommonConfiguration(1, 100);

                var netconfig = new NetConfiguration("localhost", proxyServer, "testService", 10);
                var toconfig = new ProxyConfiguration(TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));

                var proxy = new TestGate(netconfig, toconfig, common);

                var distrNet = new DistributorNetConfiguration("localhost", distrServer1, distrServer12, "testService", 10);
                var distrConf = new DistributorConfiguration(1, filename,TimeSpan.FromMilliseconds(100000), 
                    TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(10000));

                var distr = new DistributorApi(distrNet, distrConf, common);

                var storageNet = new StorageNetConfiguration("localhost", st1, st2, "testService", 10);
                var storageConfig = new StorageConfiguration(filename, 1, 10, TimeSpan.FromHours(1), TimeSpan.FromHours(1),
                    TimeSpan.FromHours(1), TimeSpan.FromHours(1), false);

                var storage = new WriterApi(storageNet, storageConfig, common);
                var async = new AsyncTaskModule(new QueueConfiguration(4, 10));
                var serversModel = new CollectorModel(new DistributorHashConfiguration(1),
                    new HashMapConfiguration(filename, HashMapCreationMode.ReadFromFile, 1, 1, HashFileType.Collector));

                var distributor = new DistributorModule(serversModel, async, new AsyncTasksConfiguration(TimeSpan.FromMinutes(1)));

                var net = new CollectorNetModule(new ConnectionConfiguration("testService", 10),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout), distributor);

                distributor.SetNetModule(net);

                var back = new BackgroundModule(new QueueConfiguration(5, 10));
                var loader = new DataLoader(net, 100, back);

                var parser = new TestIntParser();
                parser.SetCommandsHandler(new UserCommandsHandler
                    <TestCommand, Type, TestCommand, int, int, TestDbReader>(
                    new TestUserCommandCreator(), new TestMetaDataCommandCreator()));
                var merge = new OrderMerge(loader, parser, serversModel);

                var searchModule = new SearchTaskModule("Int", merge, loader, distributor, back, parser);

                q1.Start();
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

                var result = proxy.Int.SayIAmHere("localhost", distrServer1);

                const int count = 20;

                for (int i = 0; i < count; i++)
                {
                    var request = proxy.Int.CreateSync(i, i);
                    Assert.Equal(RequestState.Complete, request.State);
                }

                var reader = searchModule.CreateReader("asc", -1, 20);
                reader.Start();

                for (int i = 0; i < count; i++)
                {
                    Assert.True(reader.IsCanRead);

                    reader.ReadNext();

                    Assert.Equal(i, reader.GetValue(0));
                }
                reader.ReadNext();
                Assert.False(reader.IsCanRead);

                reader.Dispose();
                back.Dispose();
                loader.Dispose();
                net.Dispose();

                storage.Dispose();
                proxy.Dispose();
                distr.Dispose();
                async.Dispose();

                q1.Dispose();
            }
        }

        [Fact]
        public void SqlScriptParser_ParseQueryType()
        {
            var parser = new SqlScriptParser();
            var type = parser.ParseQueryType("order by");
            Assert.Equal(ScriptType.OrderAsc, type);
            type = parser.ParseQueryType("order by desc");
            Assert.Equal(ScriptType.OrderDesc, type);
            type = parser.ParseQueryType("order bsy desc");
            Assert.Equal(ScriptType.Unknown, type);
        }        
    }
}

﻿using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Tests.Support;
using Xunit;

namespace Qoollo.Tests
{    
    public class TestHashUpdate:TestBase
    {
        [Fact]
        public void HashFileUpdater_UpdateFile_AddNewFileAndRenameOld()
        {
            const string fileNameWithouPrefix = "testHashForUpdate";
            string fileName = fileNameWithouPrefix + ".txt";
            const string testMessage = "TestMessage";

            using (new FileCleaner(fileNameWithouPrefix + "1.txt"))
            using (new FileCleaner(fileNameWithouPrefix + "2.txt"))
            {
                using (var writer = new StreamWriter(fileName))
                {
                    writer.WriteLine(testMessage);
                }

                HashFileUpdater.UpdateFile(fileName);
                using (var writer = new StreamWriter(fileName))
                {
                    writer.WriteLine(testMessage);
                    writer.WriteLine(testMessage);
                }
                HashFileUpdater.UpdateFile(fileName);

                Assert.True(File.Exists(fileNameWithouPrefix + "1.txt"));
                Assert.True(File.Exists(fileNameWithouPrefix + "2.txt"));

                using (var reader = new StreamReader(fileNameWithouPrefix + "2.txt"))
                {
                    Assert.Equal(testMessage, reader.ReadLine());
                }

                using (var reader = new StreamReader(fileNameWithouPrefix + "1.txt"))
                {
                    Assert.Equal(testMessage, reader.ReadLine());
                    Assert.Equal(testMessage, reader.ReadLine());
                }
            }
        }

        [Fact]
        public void HashFileUpdater_UpdateFile_EmptyPrefix_AddNewFileAndRenameOld()
        {
            const string fileNameWithouPrefix = "testHashForUpdate";
            string fileName = fileNameWithouPrefix;
            const string testMessage = "TestMessage";

            using (new FileCleaner(fileNameWithouPrefix + "1"))
            using (new FileCleaner(fileNameWithouPrefix + "2"))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                using (var writer = new StreamWriter(fileName))
                {
                    writer.WriteLine(testMessage);
                }

                HashFileUpdater.UpdateFile(fileName);
                using (var writer = new StreamWriter(fileName))
                {
                    writer.WriteLine(testMessage);
                    writer.WriteLine(testMessage);
                }
                HashFileUpdater.UpdateFile(fileName);

                Assert.True(File.Exists(fileNameWithouPrefix + "1"));
                Assert.True(File.Exists(fileNameWithouPrefix + "2"));

                using (var reader = new StreamReader(fileNameWithouPrefix + "2"))
                {
                    Assert.Equal(testMessage, reader.ReadLine());
                }

                using (var reader = new StreamReader(fileNameWithouPrefix + "1"))
                {
                    Assert.Equal(testMessage, reader.ReadLine());
                    Assert.Equal(testMessage, reader.ReadLine());
                }
            }
        }

        [Fact]
        public void Distributor_UpdateHashOnWritersViaNet_NewServerNotExist()
        {
            const string hashFileName = "Distributor_UpdateHashOnWritersViaNet";
            const string hashFileNameWriter1 = "Distributor_UpdateHashOnWritersViaNet_1Writer";
            const string hashFileNameWriter2 = "Distributor_UpdateHashOnWritersViaNet_2Writer";

            using (new FileCleaner(hashFileNameWriter1 + "1"))
            using (new FileCleaner(hashFileNameWriter1 + "2"))
            using (new FileCleaner(hashFileNameWriter2 + "1"))
            using (new FileCleaner(hashFileNameWriter2 + "2"))
            using (new FileCleaner(hashFileName))
            using (new FileCleaner(hashFileNameWriter1))
            using (new FileCleaner(hashFileNameWriter2))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                var writer =
                   new HashWriter(new HashMapConfiguration(hashFileName, HashMapCreationMode.CreateNew, 2, 3,
                       HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                writer =
                   new HashWriter(new HashMapConfiguration(hashFileNameWriter1, HashMapCreationMode.CreateNew, 2, 3,
                       HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                writer =
                   new HashWriter(new HashMapConfiguration(hashFileNameWriter2, HashMapCreationMode.CreateNew, 2, 3,
                       HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                _distrTest.Build(1, distrServer1, distrServer12, hashFileName);

                _writer1.Build(storageServer1, hashFileNameWriter1, 1);
                _writer2.Build(storageServer2, hashFileNameWriter2, 1);

                _distrTest.Start();
                _writer1.Start();
                _writer2.Start();

                writer =
                   new HashWriter(new HashMapConfiguration(hashFileName, HashMapCreationMode.CreateNew, 3, 3,
                       HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.SetServer(2, "localhost", storageServer3, 157);
                writer.Save();

                _distrTest.Distributor.UpdateModel();

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                Assert.True(File.Exists(hashFileNameWriter1));
                Assert.True(File.Exists(hashFileNameWriter1 + "1"));
                Assert.True(File.Exists(hashFileNameWriter2));
                Assert.True(File.Exists(hashFileNameWriter2 + "1"));

                Assert.Equal(RestoreState.FullRestoreNeed, _writer1.Distributor.GetRestoreRequiredState());
                Assert.Equal(RestoreState.FullRestoreNeed, _writer2.Distributor.GetRestoreRequiredState());

                Assert.Equal(3, _writer1.WriterModel.Servers.Count);
                Assert.Equal(3, _writer2.WriterModel.Servers.Count);

                Assert.True(_distrTest.WriterSystemModel.Servers.Exists(x => !x.IsAvailable));

                _writer1.Dispose();
                _writer2.Dispose();
                _distrTest.Dispose();
            }
        }

        [Fact]
        public void Distributor_UpdateHashOnWritersViaNet_NewServerExist()
        {
            const string hashFileName = "Distributor_UpdateHashOnWritersViaNet2";
            const string hashFileNameWriter1 = "Distributor_UpdateHashOnWritersViaNet2_1Writer";
            const string hashFileNameWriter2 = "Distributor_UpdateHashOnWritersViaNet2_2Writer";
            const string hashFileNameWriter3 = "Distributor_UpdateHashOnWritersViaNet2_3Writer";

            using (new FileCleaner(hashFileNameWriter1 + "1"))
            using (new FileCleaner(hashFileNameWriter1 + "2"))
            using (new FileCleaner(hashFileNameWriter2 + "1"))
            using (new FileCleaner(hashFileNameWriter2 + "2"))
            using (new FileCleaner(hashFileNameWriter3 + "1"))
            using (new FileCleaner(hashFileNameWriter3 + "2"))
            using (new FileCleaner(hashFileName))
            using (new FileCleaner(hashFileNameWriter1))
            using (new FileCleaner(hashFileNameWriter2))
            using (new FileCleaner(hashFileNameWriter3))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                var writer =
               new HashWriter(new HashMapConfiguration(hashFileName, HashMapCreationMode.CreateNew, 2, 3,
                   HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                writer =
                   new HashWriter(new HashMapConfiguration(hashFileNameWriter1, HashMapCreationMode.CreateNew, 2, 3,
                       HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                writer =
                   new HashWriter(new HashMapConfiguration(hashFileNameWriter2, HashMapCreationMode.CreateNew, 2, 3,
                       HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                writer =
                   new HashWriter(new HashMapConfiguration(hashFileNameWriter3, HashMapCreationMode.CreateNew, 2, 3,
                       HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer3, 157);
                writer.Save();

                _distrTest.Build(1, distrServer1, distrServer12, hashFileName);

                _writer1.Build(storageServer1, hashFileNameWriter1, 1);
                _writer2.Build(storageServer2, hashFileNameWriter2, 1);
                _writer3.Build(storageServer3, hashFileNameWriter3, 1);

                _distrTest.Start();
                _writer1.Start();
                _writer2.Start();
                _writer3.Start();

                writer =
                   new HashWriter(new HashMapConfiguration(hashFileName, HashMapCreationMode.CreateNew, 3, 3,
                       HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.SetServer(2, "localhost", storageServer3, 157);
                writer.Save();

                _distrTest.Distributor.UpdateModel();

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                Assert.True(File.Exists(hashFileNameWriter1));
                Assert.True(File.Exists(hashFileNameWriter1 + "1"));
                Assert.True(File.Exists(hashFileNameWriter2));
                Assert.True(File.Exists(hashFileNameWriter2 + "1"));
                Assert.True(File.Exists(hashFileNameWriter3));
                Assert.True(File.Exists(hashFileNameWriter3 + "1"));

                Assert.Equal(RestoreState.FullRestoreNeed, _writer1.Distributor.GetRestoreRequiredState());
                Assert.Equal(RestoreState.FullRestoreNeed, _writer2.Distributor.GetRestoreRequiredState());
                Assert.Equal(RestoreState.FullRestoreNeed, _writer3.Distributor.GetRestoreRequiredState());

                Assert.Equal(3, _writer1.WriterModel.Servers.Count);
                Assert.Equal(3, _writer2.WriterModel.Servers.Count);
                Assert.Equal(3, _writer3.WriterModel.Servers.Count);

                Assert.False(_distrTest.WriterSystemModel.Servers.Exists(x => !x.IsAvailable));

                _writer1.Dispose();
                _writer2.Dispose();
                _writer3.Dispose();
                _distrTest.Dispose();
            }
        }

        [Fact]
        public void Distributor_UpdateHashOnDistributor()
        {
            const string hashFileName = "Distributor_UpdateHashOnDistributor";
            const string hashFileName2 = "Distributor3_UpdateHashOnDistributor";
            const string hashFileNameWriter1 = "Distributor_1UpdateHashOnDistributor";
            const string hashFileNameWriter2 = "Distributor_2UpdateHashOnDistributor";

            using (new FileCleaner(hashFileNameWriter1 + "1"))
            using (new FileCleaner(hashFileNameWriter1 + "2"))
            using (new FileCleaner(hashFileNameWriter2 + "1"))
            using (new FileCleaner(hashFileNameWriter2 + "2"))
            using (new FileCleaner(hashFileName2 + "1"))
            using (new FileCleaner(hashFileName2 + "2"))
            using (new FileCleaner(hashFileName))
            using (new FileCleaner(hashFileName2))
            using (new FileCleaner(hashFileNameWriter1))
            using (new FileCleaner(hashFileNameWriter2))
            using (new FileCleaner(Consts.RestoreHelpFile))
            {
                var writer =
               new HashWriter(new HashMapConfiguration(hashFileName, HashMapCreationMode.CreateNew, 2, 3,
                   HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                writer =
                   new HashWriter(new HashMapConfiguration(hashFileName2, HashMapCreationMode.CreateNew, 2, 3,
                       HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                writer =
                   new HashWriter(new HashMapConfiguration(hashFileNameWriter1, HashMapCreationMode.CreateNew, 2, 3,
                       HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                writer =
                   new HashWriter(new HashMapConfiguration(hashFileNameWriter2, HashMapCreationMode.CreateNew, 2, 3,
                       HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.Save();

                var distrTest2 = new TestDistributorGate();

                _distrTest.Build(1, distrServer1, distrServer12, hashFileName);
                distrTest2.Build(1, distrServer2, distrServer22, hashFileName2);

                _writer1.Build(storageServer1, hashFileNameWriter1, 1);
                _writer2.Build(storageServer2, hashFileNameWriter2, 1);

                _distrTest.Start();
                distrTest2.Start();

                _writer1.Start();
                _writer2.Start();

                var result = distrTest2.Distributor.SayIAmHereRemoteResult(new ServerId("localhost", distrServer1));
                Assert.False(result.IsError);

                writer =
                   new HashWriter(new HashMapConfiguration(hashFileName, HashMapCreationMode.CreateNew, 3, 3,
                       HashFileType.Distributor));
                writer.CreateMap();
                writer.SetServer(0, "localhost", storageServer1, 157);
                writer.SetServer(1, "localhost", storageServer2, 157);
                writer.SetServer(2, "localhost", storageServer3, 157);
                writer.Save();

                _distrTest.Distributor.UpdateModel();

                Thread.Sleep(TimeSpan.FromMilliseconds(1000));

                Assert.True(File.Exists(hashFileName2));
                Assert.True(File.Exists(hashFileName2 + "1"));

                Assert.Equal(3, distrTest2.WriterSystemModel.Servers.Count);

                _writer1.Dispose();
                _writer2.Dispose();
                _distrTest.Dispose();
                distrTest2.Dispose();
            }
        }        
    }
}

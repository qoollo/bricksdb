using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;
using Qoollo.Impl.Configurations;
using Qoollo.Tests.Support;

namespace Qoollo.Tests
{    
    [TestClass]
    public class TestHashUpdate:TestBase
    {

        [TestMethod]
        public void HashFileUpdater_UpdateFile_AddNewFileAndRenameOld()
        {
            const string fileNameWithouPrefix = "testHashForUpdate";
            string fileName = fileNameWithouPrefix + ".txt";
            const string testMessage = "TestMessage";

            using (var writer = new StreamWriter(fileName))
            {
                writer.WriteLine(testMessage);
            }

            File.Delete(fileNameWithouPrefix + "1.txt");
            File.Delete(fileNameWithouPrefix + "2.txt");

            HashFileUpdater.UpdateFile(fileName);
            using (var writer = new StreamWriter(fileName))
            {
                writer.WriteLine(testMessage);
                writer.WriteLine(testMessage);
            }
            HashFileUpdater.UpdateFile(fileName);

            Assert.IsTrue(File.Exists(fileNameWithouPrefix + "1.txt"));
            Assert.IsTrue(File.Exists(fileNameWithouPrefix + "2.txt"));

            using (var reader = new StreamReader(fileNameWithouPrefix + "2.txt"))
            {
                Assert.AreEqual(testMessage, reader.ReadLine());
            }

            using (var reader = new StreamReader(fileNameWithouPrefix + "1.txt"))
            {
                Assert.AreEqual(testMessage, reader.ReadLine());
                Assert.AreEqual(testMessage, reader.ReadLine());
            }

            File.Delete(fileNameWithouPrefix + "1.txt");
            File.Delete(fileNameWithouPrefix + "2.txt");
        }

        [TestMethod]
        public void HashFileUpdater_UpdateFile_EmptyPrefix_AddNewFileAndRenameOld()
        {
            const string fileNameWithouPrefix = "testHashForUpdate";
            string fileName = fileNameWithouPrefix;
            const string testMessage = "TestMessage";

            using (var writer = new StreamWriter(fileName))
            {
                writer.WriteLine(testMessage);
            }

            File.Delete(fileNameWithouPrefix + "1");
            File.Delete(fileNameWithouPrefix + "2");

            HashFileUpdater.UpdateFile(fileName);
            using (var writer = new StreamWriter(fileName))
            {
                writer.WriteLine(testMessage);
                writer.WriteLine(testMessage);
            }
            HashFileUpdater.UpdateFile(fileName);

            Assert.IsTrue(File.Exists(fileNameWithouPrefix + "1"));
            Assert.IsTrue(File.Exists(fileNameWithouPrefix + "2"));

            using (var reader = new StreamReader(fileNameWithouPrefix + "2"))
            {
                Assert.AreEqual(testMessage, reader.ReadLine());
            }

            using (var reader = new StreamReader(fileNameWithouPrefix + "1"))
            {
                Assert.AreEqual(testMessage, reader.ReadLine());
                Assert.AreEqual(testMessage, reader.ReadLine());
            }
        }

        [TestMethod]
        public void Distributor_UpdateHashOnWritersViaNet_NewServerNotExist()
        {
            const string hashFileName = "Distributor_UpdateHashOnWritersViaNet";
            const string hashFileNameWriter1 = "Distributor_UpdateHashOnWritersViaNet_1Writer";
            const string hashFileNameWriter2 = "Distributor_UpdateHashOnWritersViaNet_2Writer";

            File.Delete(hashFileNameWriter1 + "1");
            File.Delete(hashFileNameWriter1 + "2");
            File.Delete(hashFileNameWriter2 + "1");
            File.Delete(hashFileNameWriter2 + "2");

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

            Assert.IsTrue(File.Exists(hashFileNameWriter1));
            Assert.IsTrue(File.Exists(hashFileNameWriter1 + "1"));
            Assert.IsTrue(File.Exists(hashFileNameWriter2));
            Assert.IsTrue(File.Exists(hashFileNameWriter2 + "1"));

            Assert.AreEqual(RestoreState.FullRestoreNeed, _writer1.Distributor.GetRestoreRequiredState());
            Assert.AreEqual(RestoreState.FullRestoreNeed, _writer2.Distributor.GetRestoreRequiredState());
            
            Assert.AreEqual(3, _writer1.WriterModel.Servers.Count);
            Assert.AreEqual(3, _writer2.WriterModel.Servers.Count);

            Assert.IsTrue(_distrTest.WriterSystemModel.Servers.Exists(x => !x.IsAvailable));

            _writer1.Dispose();
            _writer2.Dispose();
            _distrTest.Dispose();
        }

        [TestMethod]
        public void Distributor_UpdateHashOnWritersViaNet_NewServerExist()
        {
            const string hashFileName = "Distributor_UpdateHashOnWritersViaNet2";
            const string hashFileNameWriter1 = "Distributor_UpdateHashOnWritersViaNet2_1Writer";
            const string hashFileNameWriter2 = "Distributor_UpdateHashOnWritersViaNet2_2Writer";
            const string hashFileNameWriter3 = "Distributor_UpdateHashOnWritersViaNet2_3Writer";

            File.Delete(hashFileNameWriter1 + "1");
            File.Delete(hashFileNameWriter1 + "2");
            File.Delete(hashFileNameWriter2 + "1");
            File.Delete(hashFileNameWriter2 + "2");
            File.Delete(hashFileNameWriter3 + "1");
            File.Delete(hashFileNameWriter3 + "2");

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

            Assert.IsTrue(File.Exists(hashFileNameWriter1));
            Assert.IsTrue(File.Exists(hashFileNameWriter1 + "1"));
            Assert.IsTrue(File.Exists(hashFileNameWriter2));
            Assert.IsTrue(File.Exists(hashFileNameWriter2 + "1"));
            Assert.IsTrue(File.Exists(hashFileNameWriter3));
            Assert.IsTrue(File.Exists(hashFileNameWriter3 + "1"));

            Assert.AreEqual(RestoreState.FullRestoreNeed, _writer1.Distributor.GetRestoreRequiredState());
            Assert.AreEqual(RestoreState.FullRestoreNeed, _writer2.Distributor.GetRestoreRequiredState());
            Assert.AreEqual(RestoreState.FullRestoreNeed, _writer3.Distributor.GetRestoreRequiredState());

            Assert.AreEqual(3, _writer1.WriterModel.Servers.Count);
            Assert.AreEqual(3, _writer2.WriterModel.Servers.Count);
            Assert.AreEqual(3, _writer3.WriterModel.Servers.Count);

            Assert.IsFalse(_distrTest.WriterSystemModel.Servers.Exists(x => !x.IsAvailable));

            _writer1.Dispose();
            _writer2.Dispose();
            _writer3.Dispose();
            _distrTest.Dispose();
        }

        [TestMethod]
        public void Distributor_UpdateHashOnDistributor()
        {
            const string hashFileName = "Distributor_UpdateHashOnDistributor";
            const string hashFileName2 = "Distributor3_UpdateHashOnDistributor";
            const string hashFileNameWriter1 = "Distributor_1UpdateHashOnDistributor";
            const string hashFileNameWriter2 = "Distributor_2UpdateHashOnDistributor";

            File.Delete(hashFileNameWriter1 + "1");
            File.Delete(hashFileNameWriter1 + "2");
            File.Delete(hashFileNameWriter2 + "1");
            File.Delete(hashFileNameWriter2 + "2");
            File.Delete(hashFileName2 + "1");
            File.Delete(hashFileName2 + "2");

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
            Assert.IsFalse(result.IsError);

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

            Assert.IsTrue(File.Exists(hashFileName2));
            Assert.IsTrue(File.Exists(hashFileName2 + "1"));

            Assert.AreEqual(3, distrTest2.WriterSystemModel.Servers.Count);            

            _writer1.Dispose();
            _writer2.Dispose();
            _distrTest.Dispose();
            distrTest2.Dispose();
        }        
    }
}

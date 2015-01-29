using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Client.Support;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.Writer;
using Qoollo.Impl.Writer.AsyncDbWorks;
using Qoollo.Impl.Writer.Db;
using Qoollo.Impl.Writer.Distributor;
using Qoollo.Impl.Writer.WriterNet;
using Qoollo.Tests.TestWriter;

namespace Qoollo.Tests.Support
{
    class TestWriterGate
    {
        private NetWriterReceiver _netRc;
        public InputModule Input;
        private MainLogicModule _mainС;
        public DistributorModule Distributor { get; set; }
        public AsyncDbWorkModule Restore { get; set; }
        private AsyncTaskModule _async;
        public DbModuleCollection Db { get; set; }
        private WriterNetModule _net;
        public GlobalQueueInner Q { get; set; }

        public void Build(int storageServer, string hashFile, int countReplics)
        {
            Q = new GlobalQueueInner();
            GlobalQueue.SetQueue(Q);

            var queueConfiguration = new QueueConfiguration(1, 1000);
            var hashMapConfiguration = new HashMapConfiguration(hashFile,
                HashMapCreationMode.ReadFromFile, 1, countReplics, HashFileType.Writer);
            var local = new ServerId("localhost", storageServer);

            _net = new WriterNetModule(new ConnectionConfiguration("testService", 10),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));

            Db = new DbModuleCollection();
            Db.AddDbModule(new TestDbInMemory());

            _async = new AsyncTaskModule(new QueueConfiguration(1, 10));
            Restore = new AsyncDbWorkModule(_net, _async, Db,
                new RestoreModuleConfiguration(10, TimeSpan.FromMinutes(100)),
                new RestoreModuleConfiguration(10, TimeSpan.FromMilliseconds(100)),
                new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)),
                new QueueConfiguration(1, 100), local);

            Distributor = new DistributorModule(_async, Restore, _net, local,
                hashMapConfiguration, new QueueConfiguration(2, 10), Db);
            _mainС = new MainLogicModule(Distributor, Db);
            Input = new InputModule(_mainС, queueConfiguration);
            _netRc = new NetWriterReceiver(Input, Distributor,
                new NetReceiverConfiguration(storageServer, "localhost", "testService"),
                new NetReceiverConfiguration(1, "fake", "fake"));
        }

        public void Start()
        {
            Input.Start();
            Distributor.Start();
            _netRc.Start();
            _async.Start();
            Q.Start();
        }

        public void Dispose()
        {
            _netRc.Dispose();
            Q.Dispose();
            Distributor.Dispose();
            _async.Dispose();            
            _net.Dispose();                        
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Client.Support;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DistributorModules;
using Qoollo.Impl.DistributorModules.Caches;
using Qoollo.Impl.DistributorModules.DistributorNet;
using Qoollo.Impl.DistributorModules.ParallelWork;
using Qoollo.Impl.DistributorModules.Transaction;
using Qoollo.Impl.Modules.Queue;

namespace Qoollo.Tests.Support
{
    class TestDistributorGate
    {
        private GlobalQueueInner _q;
        private DistributorNetModule _dnet;
        public DistributorModule Distributor { get; set; }
        private TransactionModule _tranc;
        public MainLogicModule Main { get; set; }
        public InputModuleWithParallel Input { get; set; }
        private NetDistributorReceiver _receiver;

        public void Build(int countReplics, int distrServer1, int distrServer12, string hashFile)
        {
            _q = new GlobalQueueInner();
            GlobalQueue.SetQueue(_q);

            var connection = new ConnectionConfiguration("testService", 10);

            var distrconfig = new DistributorHashConfiguration(countReplics);
            var queueconfig = new QueueConfiguration(1, 100);
            _dnet = new DistributorNetModule(connection,
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            Distributor = new DistributorModule(new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)),
                new AsyncTasksConfiguration(TimeSpan.FromMinutes(5)), distrconfig,
                queueconfig, _dnet,
                new ServerId("localhost", distrServer1),
                new ServerId("localhost", distrServer12),
                new HashMapConfiguration(hashFile,
                    HashMapCreationMode.ReadFromFile,
                    1, countReplics, HashFileType.Distributor));

            _dnet.SetDistributor(Distributor);

            _tranc = new TransactionModule(new QueueConfiguration(1, 1000), _dnet, new TransactionConfiguration(4),
                distrconfig);
            Main =
                new MainLogicModule(new DistributorTimeoutCache(TimeSpan.FromSeconds(200), TimeSpan.FromSeconds(200)),
                    Distributor, _tranc);

            var netReceive1 = new NetReceiverConfiguration(distrServer1, "localhost", "testService");
            var netReceive2 = new NetReceiverConfiguration(distrServer12, "localhost", "testService");
            Input = new InputModuleWithParallel(new QueueConfiguration(2, 100000), Main, _tranc);
            _receiver = new NetDistributorReceiver(Main, Input, Distributor, netReceive1, netReceive2);      
        }

        public void Build(int countReplics, string hashFile)
        {
            Build(countReplics, 22201, 22202, hashFile);
        }

        public void Start()
        {
            Main.Start();
            _receiver.Start();
            Input.Start();
            _dnet.Start();
            Distributor.Start();

            _q.Start();
        }

        public void Dispose()
        {
            Main.Dispose();
            _receiver.Dispose();
            Input.Dispose();
            _dnet.Dispose();
            Distributor.Dispose();

            _q.Dispose();
        }
    }
}

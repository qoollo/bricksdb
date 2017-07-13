using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Ninject;
using Qoollo.Client.Support;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DistributorModules;
using Qoollo.Impl.DistributorModules.Caches;
using Qoollo.Impl.DistributorModules.DistributorNet;
using Qoollo.Impl.DistributorModules.Interfaces;
using Qoollo.Impl.DistributorModules.Model;
using Qoollo.Impl.DistributorModules.ParallelWork;
using Qoollo.Impl.DistributorModules.Transaction;
using Qoollo.Impl.Modules.Config;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Tests.NetMock;

namespace Qoollo.Tests.Support
{
    internal class TestDistributorGate
    {
        private GlobalQueue _q;
        private DistributorNetModule _dnet;
        public DistributorModule Distributor { get; set; }
        private TransactionModule _tranc;
        public MainLogicModule Main { get; set; }
        public InputModuleWithParallel Input { get; set; }
        private NetDistributorReceiver _receiver;

        public WriterSystemModel WriterSystemModel { get; private set; }

        private TRet GetPrivtaeField<TRet>(object obj) where TRet : class
        {
            var list = obj.GetType().GetFields(BindingFlags.Public |
                                               BindingFlags.NonPublic |
                                               BindingFlags.Instance);

            return list.First(x => x.FieldType.FullName == typeof(TRet).ToString()).GetValue(obj) as TRet;
        }

        public void Build(int countReplics, int distrServer1, int distrServer12, string hashFile,
            TimeSpan asyncCheck = default(TimeSpan), bool autoRestoreEnable = false)
        {            
            var kernel = new StandardKernel(new TestInjectionModule());

            var config = new SettingsModule(kernel, Impl.Common.Support.Consts.ConfigFilename);
            config.Start();

            _q = new GlobalQueue(kernel);
            kernel.Bind<IGlobalQueue>().ToConstant(_q);

            asyncCheck = asyncCheck == default(TimeSpan) ? TimeSpan.FromMinutes(5) : asyncCheck;

            var connection = new ConnectionConfiguration("testService", 10);

            var distrconfig = new DistributorHashConfiguration(countReplics);
            _dnet = new DistributorNetModule(kernel, connection,
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            kernel.Bind<IDistributorNetModule>().ToConstant(_dnet);

            Distributor = new DistributorModule(kernel, new AsyncTasksConfiguration(TimeSpan.FromMilliseconds(200)),
                new AsyncTasksConfiguration(asyncCheck), distrconfig, 
                new ServerId("localhost", distrServer1),
                new ServerId("localhost", distrServer12),
                new HashMapConfiguration(hashFile,
                    HashMapCreationMode.ReadFromFile,
                    1, countReplics, HashFileType.Distributor), autoRestoreEnable);
            kernel.Bind<IDistributorModule>().ToConstant(Distributor);

            WriterSystemModel = GetPrivtaeField<WriterSystemModel>(Distributor);

            var cache = new DistributorTimeoutCache(
                new DistributorCacheConfiguration(TimeSpan.FromSeconds(200), TimeSpan.FromSeconds(200)));
            kernel.Bind<IDistributorTimeoutCache>().ToConstant(cache);

            _tranc = new TransactionModule(kernel, new TransactionConfiguration(4), distrconfig.CountReplics);
            kernel.Bind<ITransactionModule>().ToConstant(_tranc);

            Main = new MainLogicModule(kernel);
            kernel.Bind<IMainLogicModule>().ToConstant(Main);

            var netReceive1 = new NetReceiverConfiguration(distrServer1, "localhost", "testService");
            var netReceive2 = new NetReceiverConfiguration(distrServer12, "localhost", "testService");
            //, new QueueConfiguration(2, 100000)
            //todo q
            Input = new InputModuleWithParallel(kernel);
            kernel.Bind<IInputModule>().ToConstant(Input);

            _receiver = new NetDistributorReceiver(kernel, netReceive1, netReceive2);
        }

        public void Build(int countReplics, string hashFile)
        {
            Build(countReplics, 22201, 22202, hashFile);
        }

        public void Start()
        {
            _tranc.Start();
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
            _tranc.Dispose();
        }
    }
}

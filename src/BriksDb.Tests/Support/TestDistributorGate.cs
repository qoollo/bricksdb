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

        public void Build(int distrServer1, int distrServer12,
            TimeSpan asyncCheck = default(TimeSpan), bool autoRestoreEnable = false, 
            string configFile = Impl.Common.Support.Consts.ConfigFilename)
        {            
            var kernel = new StandardKernel(new TestInjectionModule());

            var config = new SettingsModule(kernel, configFile);
            config.Start();

            _q = new GlobalQueue(kernel);
            kernel.Bind<IGlobalQueue>().ToConstant(_q);

            asyncCheck = asyncCheck == default(TimeSpan) ? TimeSpan.FromMinutes(5) : asyncCheck;

            _dnet = new DistributorNetModule(kernel,
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            kernel.Bind<IDistributorNetModule>().ToConstant(_dnet);

            Distributor = new DistributorModule(kernel, new AsyncTasksConfiguration(TimeSpan.FromMilliseconds(200)),
                new AsyncTasksConfiguration(asyncCheck),
                new ServerId("localhost", distrServer1),
                new ServerId("localhost", distrServer12),
                autoRestoreEnable);
            kernel.Bind<IDistributorModule>().ToConstant(Distributor);

            var cache = new DistributorTimeoutCache(
                new DistributorCacheConfiguration(TimeSpan.FromSeconds(200), TimeSpan.FromSeconds(200)));
            kernel.Bind<IDistributorTimeoutCache>().ToConstant(cache);

            //new TransactionConfiguration(4),
            _tranc = new TransactionModule(kernel);
            kernel.Bind<ITransactionModule>().ToConstant(_tranc);

            Main = new MainLogicModule(kernel);
            kernel.Bind<IMainLogicModule>().ToConstant(Main);

            var netReceive1 = new NetReceiverConfiguration(distrServer1, "localhost", "testService");
            var netReceive2 = new NetReceiverConfiguration(distrServer12, "localhost", "testService");

            Input = new InputModuleWithParallel(kernel);
            kernel.Bind<IInputModule>().ToConstant(Input);

            _receiver = new NetDistributorReceiver(kernel, netReceive1, netReceive2);
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

            WriterSystemModel = GetPrivtaeField<WriterSystemModel>(Distributor);
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

using System;
using Ninject;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Modules.Config;
using Qoollo.Impl.Modules.Interfaces;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.Writer;
using Qoollo.Impl.Writer.AsyncDbWorks;
using Qoollo.Impl.Writer.Db;
using Qoollo.Impl.Writer.Interfaces;
using Qoollo.Impl.Writer.WriterNet;
using Qoollo.Tests.NetMock;
using Qoollo.Tests.TestWriter;

namespace Qoollo.Tests.Support
{
    internal class TestWriterGate
    {
        private NetWriterReceiver _netRc;
        public InputModule Input;
        private MainLogicModule _mainС;
        public DistributorModule Distributor { get; set; }

        public WriterModel WriterModel { get; private set; }
        public AsyncDbWorkModule Restore { get; set; }
        private AsyncTaskModule _async;
        public DbModuleCollection Db { get; set; }
        private WriterNetModule _net;
        private StandardKernel _kernel;
        public GlobalQueue Q { get; set; }

        public void Build(int storageServer, string name = "", string configFile = Impl.Common.Support.Consts.ConfigFilename)
        {
            _kernel = new StandardKernel(new TestInjectionModule());

            var config = new SettingsModule(_kernel, configFile);
            config.Start();

            Q = new GlobalQueue(_kernel, name);
            _kernel.Bind<IGlobalQueue>().ToConstant(Q);

            var local = new ServerId("localhost", storageServer);

            _net = new WriterNetModule(_kernel);
            _kernel.Bind<IWriterNetModule>().ToConstant(_net);

            Db = new DbModuleCollection(_kernel);
            _kernel.Bind<IDbModule>().ToConstant(Db);

            Db.AddDbModule(new TestDbInMemory());

            _async = new AsyncTaskModule(_kernel);
            _kernel.Bind<IAsyncTaskModule>().ToConstant(_async);

            WriterModel = new WriterModel(_kernel, local);
            _kernel.Bind<IWriterModel>().ToConstant(WriterModel);

            Restore = new AsyncDbWorkModule(_kernel, 
                new RestoreModuleConfiguration(3, TimeSpan.FromMilliseconds(300)),
                new RestoreModuleConfiguration(3, TimeSpan.FromMilliseconds(100)));
            _kernel.Bind<IAsyncDbWorkModule>().ToConstant(Restore);

            Distributor = new DistributorModule(_kernel);
            _kernel.Bind<IDistributorModule>().ToConstant(Distributor);

            _mainС = new MainLogicModule(_kernel);
            _kernel.Bind<IMainLogicModule>().ToConstant(_mainС);

            Input = new InputModule(_kernel);
            _kernel.Bind<IInputModule>().ToConstant(Input);

            _netRc = new NetWriterReceiver(_kernel);
        }

        public void Start()
        {
            WriterModel.Start();
            _net.Start();
            Input.Start();
            _mainС.Start();
            Distributor.Start();
            _netRc.Start();
            _async.Start();            
            Restore.Start();

            Q.Start();
        }

        public void Dispose()
        {
            Restore.Dispose();
            _netRc.Dispose();
            Q.Dispose();
            Distributor.Dispose();
            _async.Dispose();
            _net.Dispose();
        }
    }
}

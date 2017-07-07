using System;
using System.Linq;
using System.Reflection;
using Ninject;
using Qoollo.Client.Support;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Async;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.Writer;
using Qoollo.Impl.Writer.AsyncDbWorks;
using Qoollo.Impl.Writer.Db;
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

        private TRet GetPrivtaeField<TRet>(object obj) where TRet : class
        {
            var list = obj.GetType().GetFields(BindingFlags.Public |
                                               BindingFlags.NonPublic |
                                               BindingFlags.Instance);

            return list.First(x => x.FieldType.FullName == typeof (TRet).ToString()).GetValue(obj) as TRet;
        }

        public void Build(int storageServer, string hashFile, int countReplics, string name = "")
        {
            _kernel = new StandardKernel(new TestInjectionModule());

            Q = new GlobalQueue(name);
            _kernel.Bind<IGlobalQueue>().ToConstant(Q);

            var queueConfiguration = new QueueConfiguration(1, 1000);
            var hashMapConfiguration = new HashMapConfiguration(hashFile,
                HashMapCreationMode.ReadFromFile, 1, countReplics, HashFileType.Writer);
            var local = new ServerId("localhost", storageServer);

            _net = new WriterNetModule(_kernel, new ConnectionConfiguration("testService", 10),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));

            Db = new DbModuleCollection(_kernel);
            Db.AddDbModule(new TestDbInMemory(_kernel));

            _async = new AsyncTaskModule(_kernel, new QueueConfiguration(1, 10));
            var model = new WriterModel(_kernel, local, hashMapConfiguration);

            Restore = new AsyncDbWorkModule(_kernel, model, _net, _async, Db,
                new RestoreModuleConfiguration(3, TimeSpan.FromMilliseconds(300)),
                new RestoreModuleConfiguration(3, TimeSpan.FromMilliseconds(100)),
                new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)),
                new QueueConfiguration(1, 100));

            Distributor = new DistributorModule(_kernel, model, _async, Restore, _net, new QueueConfiguration(2, 10));

            WriterModel = GetPrivtaeField<WriterModel>(Distributor);

            _mainС = new MainLogicModule(_kernel, Distributor, Db);
            Input = new InputModule(_kernel, _mainС, queueConfiguration);
            _netRc = new NetWriterReceiver(_kernel, Input, Distributor,
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
            Restore.Start();
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

﻿using System;
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
        public GlobalQueueInner Q { get; set; }

        private TRet GetPrivtaeField<TRet>(object obj) where TRet : class
        {
            var list = obj.GetType().GetFields(BindingFlags.Public |
                                               BindingFlags.NonPublic |
                                               BindingFlags.Instance);

            return list.First(x => x.FieldType.FullName == typeof(TRet).ToString()).GetValue(obj) as TRet;
        }

        public void Build(int storageServer, string hashFile, int countReplics, string name = "")
        {
            Q = new GlobalQueueInner(name);
            GlobalQueue.SetQueue(Q);

            using (var saver = new KernelSaver())
            {
                _kernel = saver.Kernel;
                _kernel.Rebind<IGlobalQueue>().ToConstant(Q);

                var queueConfiguration = new QueueConfiguration(1, 1000);
                var hashMapConfiguration = new HashMapConfiguration(hashFile,
                    HashMapCreationMode.ReadFromFile, 1, countReplics, HashFileType.Writer);
                var local = new ServerId("localhost", storageServer);

                _net = new WriterNetModule(new ConnectionConfiguration("testService", 10),
                    new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));

                Db = new DbModuleCollection();
                Db.AddDbModule(new TestDbInMemory());

                _async = new AsyncTaskModule(new QueueConfiguration(1, 10));
                var model = new WriterModel(local, hashMapConfiguration);

                Restore = new AsyncDbWorkModule(model, _net, _async, Db,
                    new RestoreModuleConfiguration(3, TimeSpan.FromMilliseconds(300)),
                    new RestoreModuleConfiguration(3, TimeSpan.FromMilliseconds(100)),
                    new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)),
                    new QueueConfiguration(1, 100));

                Distributor = new DistributorModule(model, _async, Restore, _net, new QueueConfiguration(2, 10));

                WriterModel = GetPrivtaeField<WriterModel>(Distributor);

                _mainС = new MainLogicModule(Distributor, Db);
                Input = new InputModule(_mainС, queueConfiguration);
                _netRc = new NetWriterReceiver(Input, Distributor,
                    new NetReceiverConfiguration(storageServer, "localhost", "testService"),
                    new NetReceiverConfiguration(1, "fake", "fake"));
            }
        }

        public void Start()
        {
            using (var saver = new KernelSaver(_kernel))
            {
                Input.Start();
                Distributor.Start();
                _netRc.Start();
                _async.Start();
                Q.Start();
                Restore.Start();
            }
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

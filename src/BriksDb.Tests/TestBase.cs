using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Ninject;
using Qoollo.Client.Configuration;
using Qoollo.Client.DistributorGate;
using Qoollo.Client.Support;
using Qoollo.Client.WriterGate;
using Qoollo.Impl.Collector.Model;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Components;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.DistributorModules;
using Qoollo.Impl.DistributorModules.DistributorNet;
using Qoollo.Impl.DistributorModules.Interfaces;
using Qoollo.Impl.DistributorModules.Model;
using Qoollo.Impl.Modules.Config;
using Qoollo.Impl.Modules.Queue;
using Qoollo.Impl.Proxy;
using Qoollo.Impl.Proxy.Caches;
using Qoollo.Impl.Proxy.Interfaces;
using Qoollo.Impl.Proxy.ProxyNet;
using Qoollo.Impl.TestSupport;
using Qoollo.Tests.NetMock;
using Qoollo.Tests.Support;
using Qoollo.Tests.TestModules;
using Qoollo.Tests.TestProxy;

namespace Qoollo.Tests
{
    public class TestBase:IDisposable
    {
        internal  StandardKernel _kernel = new StandardKernel(new TestInjectionModule());

        internal TestWriterGate _writer1;
        internal TestWriterGate _writer2;
        internal TestWriterGate _writer3;
        internal TestGate _proxy;
        internal TestDistributorGate _distrTest;
        internal const int distrServer1 = 22323;
        internal const int distrServer2 = 22423;
        internal const int proxyServer = 22331;
        internal const int distrServer12 = 22324;
        internal const int distrServer22 = 22424;
        internal const int storageServer1 = 22155;
        internal const int storageServer2 = 22156;
        internal const int storageServer3 = 22157;
        internal const int storageServer4 = 22158;

        internal string file1 = "restoreHelp1.txt";
        internal string file2 = "restoreHelp2.txt";
        internal string file3 = "restoreHelp3.txt";
        internal string file4 = "restoreFile4.txt";

        internal string config_file = "config.txt";
        internal string config_file1 = "config1.txt";
        internal string config_file2 = "config2.txt";
        internal string config_file3 = "config3.txt";
        internal string config_file4 = "config4.txt";

        private readonly List<int> _writerPorts;

        private static readonly object Lock = new object();

        public TestBase()
        {
            Monitor.Enter(Lock);

            NetMock.NetMock.Instance = new NetMock.NetMock();

            CreateConfigFile();

            new SettingsModule(_kernel, Qoollo.Impl.Common.Support.Consts.ConfigFilename)
                .Start();

            InitInjection.RestoreUsePackage = false;
            InitInjection.RestoreHelpFileOut = Impl.Common.Support.Consts.RestoreHelpFile;

            _writerPorts = new List<int> {storageServer1, storageServer2, storageServer3, storageServer4};

            var netconfig = new NetConfiguration("localhost", proxyServer, "testService", 10);
            var toconfig = new ProxyConfiguration(TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(10),
                TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));

            _proxy = new TestGate(netconfig, toconfig);
            _proxy.Module = new TestInjectionModule();
            _proxy.Build();

            _distrTest = new TestDistributorGate();
            _writer1 = new TestWriterGate();
            _writer2 = new TestWriterGate();
            _writer3 = new TestWriterGate();
        }

        #region Config file

        protected void UpdateConfigReader()
        {
            new SettingsModule(_kernel, Qoollo.Impl.Common.Support.Consts.ConfigFilename)
                .Start();
        }

        protected void CreateConfigFile(string filename = Qoollo.Impl.Common.Support.Consts.ConfigFilename, 
            int distrthreads = 4, int countReplics = 2, string hash = "")
        {
            using (var writer = new StreamWriter(filename, false))
            {
                writer.WriteLine($@"{{ {GetQueue()}, {GetAsync()}, {GetDistrtibutor(distrthreads)}, {GetWriter()}, {GetCommon(countReplics, hash)} }}");
            }

            UpdateConfigReader();
        }

        private string GetAsync()
        {
            return $@"""asynctask"": {{ {GetParam("countthreads", 4)} }} ";
        }

        private string GetDistrtibutor(int distrthreads)
        {
            return "\n" + $@"""distributor"": {{ {GetParam("countthreads", distrthreads)} }} ";
        }

        private string GetCommon(int countReplice, string hash)
        {
            return "\n" +
                   $@"""common"": {{ {GetParam("countreplics", countReplice)}, {GetParam("hashfilename", hash)}, {
                       GetConnection()} }} ";
        }

        private string GetConnection()
        {
            return "\n" +
                   $@"""connection"": {{ {GetParam("servicename", "some name")}, {GetParam("countconnections", 10)}, {
                       GetParam("trimperiod", 100)} }} ";
        }

        private string GetWriter()
        {
            return
                $@"""writer"": {{ {GetParam("packagesizerestore", 1000)}, {GetParam("packagesizebroadcast", 1000)
                    }, {GetParam("packagesizetimeout", 1000)} }} ";
        }

        private string GetQueue()
        {
            return $@"""queue"": {{ 
    {GetSingle("writerdistributor")},
    {GetSingle("WriterInput")},
    {GetSingle("WriterInputRollback")},
    {GetSingle("WriterRestore")},
    {GetSingle("WriterRestorePackage")},
    {GetSingle("WriterTimeout")},
    {GetSingle("WriterTransactionAnswer")},

    {GetSingle("DistributorDistributor")},
    {GetSingle("DistributorTransaction")},
    {GetSingle("DistributorTransactionCallback")},

    {GetSingle("ProxyDistributor")},
    {GetSingle("ProxyInput")},
    {GetSingle("ProxyInputOther")},
        }} ";
        }

        private string GetSingle(string name)
        {
            return $@"""{name.ToLower()}"": {{ {GetParam("countthreads", 1)}, {GetParam("maxsize", 1000)} }}";
        }

        private string GetParam(string name, object value)
        {
            var strValue = value.ToString();
            if (value is string)
                strValue = $@"""{strValue}""";
            return $@"""{name}"":{strValue}";
        }

        #endregion

        protected void CreateHashFile(string filename, int countServers)
        {
            var writer = new HashWriter(null, filename, countServers);
            for (int i = 0; i < countServers; i++)
            {
                writer.SetServer(i, "localhost", _writerPorts[i], _writerPorts[i]);
            }
            writer.Save();
        }

        internal ServerId ServerId(int serverPort)
        {
            return new ServerId("localhost", serverPort);
        }

        internal ProxyNetModule ProxyNetModule()
        {
            var net = new ProxyNetModule(_kernel,
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            _kernel.Rebind<IProxyNetModule>().ToConstant(net);
            return net;
        }

        internal DistributorNetModule DistributorNetModule()
        {
            var net = new DistributorNetModule(_kernel,
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
            _kernel.Rebind<IDistributorNetModule>().ToConstant(net);
            return net;
        }

        internal DistributorModule DistributorDistributorModule(
            DistributorNetModule net, int pingTo = 200, int asyncCheckTo = 2000,
            int distrPort1 = distrServer1, int distrPort2 = distrServer12)
        {
            return new DistributorModule(_kernel, 
                new AsyncTasksConfiguration(TimeSpan.FromMilliseconds(pingTo)),
                new AsyncTasksConfiguration(TimeSpan.FromMilliseconds(asyncCheckTo)),
                new ServerId("localhost", distrPort1),
                new ServerId("localhost", distrPort2));
        }

        internal AsyncProxyCache AsyncProxyCache()
        {
            var cache = new AsyncProxyCache(TimeSpan.FromMinutes(100));
            _kernel.Rebind<IAsyncProxyCache>().ToConstant(cache);
            return cache;
        }

        internal ProxyDistributorModule ProxyDistributorModule(ProxyNetModule net, int proxyPort)
        {
            AsyncProxyCache();
                        
            return new ProxyDistributorModule(_kernel, ServerId(proxyPort),
                new AsyncTasksConfiguration(TimeSpan.FromDays(1)),
                new AsyncTasksConfiguration(TimeSpan.FromDays(1)));
        }

        internal CollectorModel CollectorModel()
        {
            var ret =  new CollectorModel(_kernel);
            ret.StartConfig();
            return ret;
        }

        internal TestGate TestGate(int proxyPort, int syncTo = 60)
        {
            var netconfig = new NetConfiguration("localhost", proxyPort, "testService", 10);
            var toconfig = new ProxyConfiguration(TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(syncTo),
                TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));

            return new TestGate(netconfig, toconfig);
        }

        internal DistributorConfiguration DistributorConfiguration(string filename, int countReplics)
        {
            return new DistributorConfiguration(countReplics, filename, TimeSpan.FromMilliseconds(100000),
                    TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(10000));
        }

        internal DistributorApi DistributorApi(DistributorConfiguration distrConf, int portForProxy, int portForStorage)
        {
            var distrNet = new DistributorNetConfiguration("localhost", portForProxy, portForStorage, "testService", 10);
            return new DistributorApi(distrNet, distrConf);
        }

        internal StorageConfiguration StorageConfiguration(string filename, int countReplics, 
            int restoreAnswerMls = 10000000, int deleteRestoreMls = 1000000, int periodStartDelete = 1000000, 
            bool isForceDelete = false)
        {
            return new StorageConfiguration(filename, countReplics, 10, TimeSpan.FromHours(1),
                TimeSpan.FromMilliseconds(restoreAnswerMls), 
                TimeSpan.FromMilliseconds(deleteRestoreMls), 
                TimeSpan.FromMilliseconds(periodStartDelete), isForceDelete);
        }

        internal WriterApi WriterApi(StorageConfiguration storageConfiguration, int portForDistr, int portForCollector = 157)
        {
            var storageNet = new StorageNetConfiguration("localhost", portForDistr, portForCollector, "testService", 10);
            return new WriterApi(storageNet, storageConfiguration);
        }

        internal NetReceiverConfiguration NetReceiverConfiguration(int serverPort)
        {
            return new NetReceiverConfiguration(serverPort, "localhost", "testService");
        }

        internal DistributorCacheConfiguration DistributorCacheConfiguration(int deleteMls = 2000, int updateMls = 200000)
        {
            return new DistributorCacheConfiguration(TimeSpan.FromMilliseconds(deleteMls), TimeSpan.FromMilliseconds(updateMls));
        }

        internal WriterSystemModel WriterSystemModel(int countReplics)
        {
            return new WriterSystemModel(_kernel, countReplics);
        }

        internal TestProxySystem TestProxySystem(int proxyPort, int cacheToSec = 2, int asyncCacheToSec = 2)
        {
            var pcc = new ProxyCacheConfiguration(TimeSpan.FromSeconds(cacheToSec));
            var pcc2 = new ProxyCacheConfiguration(TimeSpan.FromSeconds(asyncCacheToSec));
            return new TestProxySystem(ServerId(proxyPort),
               pcc, pcc2,
               NetReceiverConfiguration(proxyPort),
               new AsyncTasksConfiguration(new TimeSpan()),
               new AsyncTasksConfiguration(new TimeSpan()),
               new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
        }

        internal DistributorSystem DistributorSystem(DistributorCacheConfiguration cacheConfiguration,
            int portForProxy, int portForWriter,
            int toMls1 = 200, int toMls2 = 30000)
        {
            return new DistributorSystem(ServerId(portForWriter), ServerId(portForProxy),
                 cacheConfiguration,
                NetReceiverConfiguration(portForWriter),
                NetReceiverConfiguration(portForProxy),
                new AsyncTasksConfiguration(TimeSpan.FromMilliseconds(toMls1)),
                new AsyncTasksConfiguration(TimeSpan.FromMilliseconds(toMls2)),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout));
        }

        internal WriterSystem WriterSystem(int portForDistr, int portForCollector = 157)
        {
            return new WriterSystem(ServerId(portForDistr),
                NetReceiverConfiguration(portForDistr),
                NetReceiverConfiguration(portForCollector),
                new RestoreModuleConfiguration(10, new TimeSpan()),
                new RestoreModuleConfiguration(10, new TimeSpan()),
                new ConnectionTimeoutConfiguration(Consts.OpenTimeout, Consts.SendTimeout),
                new RestoreModuleConfiguration(-1, TimeSpan.FromHours(1), false, TimeSpan.FromHours(1)));
        }

        internal GlobalQueue GetBindedQueue(string name = "")
        {
            var queue = new GlobalQueue(_kernel, name);
            _kernel.Rebind<IGlobalQueue>().ToConstant(queue);
            return queue;
        }

        protected virtual void Dispose(bool isUserCall)
        {
            File.Delete(Qoollo.Impl.Common.Support.Consts.ConfigFilename);
        }

        public void Dispose()
        {
            Dispose(true);
            Monitor.Exit(Lock);
        }
    }
}

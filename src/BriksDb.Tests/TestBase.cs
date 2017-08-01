﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Ninject;
using Qoollo.Client.Configuration;
using Qoollo.Client.DistributorGate;
using Qoollo.Client.WriterGate;
using Qoollo.Impl.Collector.Model;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Components;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Configurations.Queue;
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
using DistributorConfiguration = Qoollo.Client.Configuration.DistributorConfiguration;

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
        internal const int distrServer1 = 1;
        internal const int distrServer2 = 2;
        internal const int distrServer12 = 3;
        internal const int distrServer22 = 4;
        internal const int proxyServer = 11;
        internal const int storageServer1 = 101;
        internal const int storageServer2 = 102;
        internal const int storageServer3 = 103;
        internal const int storageServer4 = 104;

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

            _proxy = new TestGate();
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
            int distrthreads = 4, int countReplics = 2, string hash = "", int distrport = storageServer1, 
            int collectorport = storageServer1, int writerport = distrServer1, int proxyport = distrServer12, 
            int pdistrport = proxyServer, int timeAliveBeforeDeleteMls = 10000, 
            int timeAliveAfterUpdateMls = 10000, int ping = 200, int check = 2000,
            int transaction= 10000, int support = 10000,
            bool isForceStart = false, int periodRetryMls = 100000, int deleteTimeoutMls= 100000)
        {
            using (var writer = new StreamWriter(filename, false))
            {
                writer.WriteLine(
                    $@"{{ {GetQueue()}, {GetAsync()}, {
                            GetDistrtibutor(distrthreads, writerport, proxyport,
                                timeAliveBeforeDeleteMls, timeAliveAfterUpdateMls, ping, check)
                        }, {GetWriter(distrport, collectorport, isForceStart, periodRetryMls, deleteTimeoutMls)}, {
                            GetCommon(countReplics, hash)
                        }, {GetProxy(pdistrport, transaction, support)},{GetCollector()} }}");
            }

            UpdateConfigReader();
        }

        private string GetAsync()
        {
            return $@"""asynctask"": {{ {GetParam("countthreads", 4)} }} ";
        }

        private string GetProxy(int distrport, int transaction, int support)
        {
            return "\n" +
                   $@"""proxy"": {{ {GetNet("netdistributor", distrport)}, {ProxyTimeouts()}, {
                           GetProxyCache(transaction, support)
                       } }} ";
        }

        private string GetProxyCache(int transaction, int support)
        {
            return $@"""cache"": {{ {GetParam("Transaction", transaction)}, {GetParam("Support", support)} }} ";
        }

        private string GetDistrtibutor(int distrthreads, int portwriter, int portproxy,
            int timeAliveBeforeDeleteMls, int timeAliveAfterUpdateMls, int ping, int check)
        {
            return "\n" +
                   $@"""distributor"": {{ {GetParam("countthreads", distrthreads)}, {GetNet("netwriter", portwriter)}, {
                           GetNet("netproxy", portproxy)
                       }, {GetDCache(timeAliveBeforeDeleteMls, timeAliveAfterUpdateMls)}, {
                           DistributorTimeouts(ping, check)
                       } }} ";
        }

        private string GetDCache(int timeAliveBeforeDeleteMls, int timeAliveAfterUpdateMls)
        {
            return $@"""cache"": {{ {GetParam("TimeAliveBeforeDeleteMls", timeAliveBeforeDeleteMls)}, {
                GetParam("TimeAliveAfterUpdateMls", timeAliveAfterUpdateMls)} }} ";
        }

        private string GetCommon(int countReplice, string hash)
        {
            return "\n" +
                   $@"""common"": {{ {GetParam("countreplics", countReplice)}, {GetParam("hashfilename", hash)}, {
                       GetConnection()}, {GetConnectionTimeout()} }} ";
        }

        private string GetConnection()
        {
            return "\n" +
                   $@"""connection"": {{ {GetParam("servicename", "some name")}, {GetParam("countconnections", 10)}, {
                       GetParam("trimperiod", 100)} }} ";
        }

        private string GetConnectionTimeout()
        {
            return "\n" +
                   $@"""connectiontimeout"": {{ {GetParam("sendtimeoutmls", 1000)}, {GetParam("opentimeoutmls", 100)} }} ";
        }

        private string GetWriter(int distrport, int collectorport, bool isForceStart, int periodRetryMls, int deleteTimeoutMls)
        {
            return
                $@"""writer"": {{ {GetParam("packagesizerestore", 1000)}, {GetParam("packagesizebroadcast", 1000)}, {
                        GetParam("packagesizetimeout", 1000)
                    }, {GetNet("netdistributor", distrport)}, {GetNet("netcollector", collectorport)}, {
                        WriterTimeouts()
                    }, {
                        GetRestore(isForceStart, periodRetryMls, deleteTimeoutMls)
                    }}} ";
        }

        private string GetRestore(bool isForceStart, int periodRetryMls, int deleteTimeoutMls)
        {
            return $@"""restore"": {{ {GetTimeout(isForceStart, periodRetryMls, deleteTimeoutMls)} }} ";
        }

        private string GetTimeout(bool isForceStart, int periodRetryMls, int deleteTimeoutMls)
        {
            return $@"""timeoutdelete"": {{ {GetParam("PeriodRetryMls", periodRetryMls)}, {
                    GetParam("ForceStart", isForceStart)
                }, {GetParam("DeleteTimeoutMls", deleteTimeoutMls)} }} ";
        }

        private string GetCollector()
        {
            return $@"""collector"": {{ {CollectorTimeouts()} }} ";
        }
        
        private string CollectorTimeouts()
        {
            return
                $@"""timeouts"": {{ {GetTimeout("ServersPingMls", 100)}, {
                    GetTimeout("DistributorUpdateHashMls", 60000)} }}";
        }

        private string WriterTimeouts()
        {
            return $@"""timeouts"": {{ {GetTimeout("ServersPingMls", 100)} }}";
        }

        private string ProxyTimeouts()
        {
            return $@"""timeouts"": {{ {GetTimeout("ServersPingMls", 10000)}, {
                    GetTimeout("DistributorUpdateInfoMls", 10000)
                } }}";
        }

        private string DistributorTimeouts(int ping, int check)
        {
            return $@"""timeouts"": {{ {GetTimeout("ServersPingMls", ping)}, {
                    GetTimeout("CheckRestoreMls", check)}, {GetTimeout("DistributorsPingMls", 10000)},{
                GetTimeout("UpdateHashMapMls", 10000)} }}";
        }

        private string GetTimeout(string name, int value)
        {
            return $@"""{name}"": {{ {GetParam("PeriodMls", value)} }}";
        }

        private string GetNet(string name, int port)
        {
            return "\n" + $@"""{name}"": {{ {GetParam("host", "localhost")}, {GetParam("port", port)} }} ";
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
            if (value is bool)
                strValue = strValue.ToLower();
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
            var net = new ProxyNetModule(_kernel);
            _kernel.Rebind<IProxyNetModule>().ToConstant(net);
            return net;
        }

        internal DistributorNetModule DistributorNetModule()
        {
            var net = new DistributorNetModule(_kernel);
            _kernel.Rebind<IDistributorNetModule>().ToConstant(net);
            return net;
        }

        internal DistributorModule DistributorDistributorModule(DistributorNetModule net)
        {
            return new DistributorModule(_kernel);
        }

        internal AsyncProxyCache AsyncProxyCache()
        {
            //TimeSpan.FromMinutes(100)
            var cache = new AsyncProxyCache(new Impl.Configurations.Queue.ProxyCacheConfiguration(1000000, 1000));
            _kernel.Rebind<IAsyncProxyCache>().ToConstant(cache);
            return cache;
        }

        internal ProxyDistributorModule ProxyDistributorModule(ProxyNetModule net)
        {
            AsyncProxyCache();
                        
            return new ProxyDistributorModule(_kernel);
        }

        internal CollectorModel CollectorModel()
        {
            var ret =  new CollectorModel(_kernel);
            ret.StartConfig();
            return ret;
        }

        internal TestGate TestGate()
        {
            //int syncTo = 60
            return new TestGate();
        }

        internal DistributorConfiguration DistributorConfiguration(string filename, int countReplics)
        {
            return new DistributorConfiguration(countReplics, filename, TimeSpan.FromMilliseconds(100000),
                    TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1), TimeSpan.FromMilliseconds(10000));
        }

        internal DistributorApi DistributorApi()
        {
            return new DistributorApi();
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

        internal WriterApi WriterApi(StorageConfiguration storageConfiguration)
        {
            return new WriterApi(storageConfiguration);
        }

        internal DistributorCacheConfiguration DistributorCacheConfiguration(int deleteMls = 2000, int updateMls = 200000)
        {
            return new DistributorCacheConfiguration(deleteMls, updateMls);
        }

        internal WriterSystemModel WriterSystemModel(int countReplics)
        {
            return new WriterSystemModel(_kernel, countReplics);
        }

        internal TestProxySystem TestProxySystem()
        {
            return new TestProxySystem();
        }

        internal DistributorSystem DistributorSystem()
        {
            return new DistributorSystem();
        }

        internal WriterSystem WriterSystem()
        {
            return new WriterSystem(
                new RestoreModuleConfiguration(10, new TimeSpan()),
                new RestoreModuleConfiguration(10, new TimeSpan()));
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

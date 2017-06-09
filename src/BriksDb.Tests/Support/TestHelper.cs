﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Ninject;
using Ninject.Parameters;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.Modules.Net;
using Qoollo.Impl.Modules.Net.ReceiveBehavior;
using Qoollo.Impl.NetInterfaces.Distributor;
using Qoollo.Impl.NetInterfaces.Writer;
using Qoollo.Impl.TestSupport;
using Qoollo.Tests.NetMock;
using Qoollo.Tests.TestModules;
using Qoollo.Tests.TestProxy;

namespace Qoollo.Tests.Support
{
    internal static class TestHelper
    {
        public static StoredData CreateStoredData(int id)
        {
            return new StoredData(id);
        }

        public  static InnerData CreateEvent(IHashCalculater calc, int id)
        {
            string hash = calc.CalculateHashFromKey(id);

            var ev = new InnerData(new Transaction(hash, "") { OperationName = OperationName.Read })
            {
                Data = calc.SerializeValue(CreateStoredData(id))
            };
            return ev;
        }        

        public static TestNetDistributorForProxy OpenDistributorHost(ServerId server, ConnectionConfiguration config)
        {
            var ret = new TestNetDistributorForProxy();
            var host = new ServiceHost(ret,
                                       new Uri(string.Format("net.tcp://{0}:{1}/{2}", server.RemoteHost, server.Port,
                                                             config.ServiceName)));
            var binding = new NetTcpBinding
            {
                Security = { Mode = SecurityMode.None },
                TransactionFlow = true
            };
            var contractType = typeof(ICommonNetReceiverForProxy);
            host.AddServiceEndpoint(contractType, binding, "");
            var behavior = host.Description.Behaviors.Find<ServiceBehaviorAttribute>();
            behavior.InstanceContextMode = InstanceContextMode.Single;

            host.Open();

            return ret;
        }

        public static TestWriterServer OpenWriterHost(ServerId server, ConnectionConfiguration config)
        {
            var ret = new TestWriterServer();

            var netConfig = new NetReceiverConfiguration(server.Port, server.RemoteHost, config.ServiceName);

            //OpenWriterNetHost(ret, netConfig);
            OpenWriterMockHost(ret, netConfig);

            return ret;
        }

        public static void OpenWriterNetHost(TestWriterServer server, NetReceiverConfiguration config)
        {
            var host = new ServiceHost(server,
                new Uri($"net.tcp://{config.Host}:{config.Port}/{config.Service}"));

            server.Host = host;
            var binding = new NetTcpBinding
            {
                Security = { Mode = SecurityMode.None },
                TransactionFlow = true
            };
            var contractType = typeof(ICommonNetReceiverWriterForWrite);
            host.AddServiceEndpoint(contractType, binding, "");
            var behavior = host.Description.Behaviors.Find<ServiceBehaviorAttribute>();
            behavior.InstanceContextMode = InstanceContextMode.Single;

            host.Open();
        }

        public static void OpenWriterMockHost(TestWriterServer server, NetReceiverConfiguration config)
        {
            var s = InitInjection.Kernel.Get<IReceiveBehavior<ICommonNetReceiverWriterForWrite>>(
                new ConstructorArgument("configuration", config),
                new ConstructorArgument("server", server));

            s.Start();
        }

        public static IDisposable OpenDistributorHostForDb(ServerId server, ConnectionConfiguration config, out TestNetDistributorForProxy distributor)
        {
            var ret = new TestNetDistributorForProxy();
            var host = new ServiceHost(ret,
                new Uri(string.Format("net.tcp://{0}:{1}/{2}", server.RemoteHost, server.Port,
                    config.ServiceName)));
            var binding = new NetTcpBinding { Security = { Mode = SecurityMode.None }, TransactionFlow = true };
            var contractType = typeof(ICommonNetReceiverForDb);
            host.AddServiceEndpoint(contractType, binding, "");
            var behavior = host.Description.Behaviors.Find<ServiceBehaviorAttribute>();
            behavior.InstanceContextMode = InstanceContextMode.Single;

            host.Open();

            distributor = ret;
            return host;
        }
        public static TestNetDistributorForProxy OpenDistributorHostForDb(ServerId server, ConnectionConfiguration config)
        {
            TestNetDistributorForProxy ret = null;
            OpenDistributorHostForDb(server, config, out ret);
            return ret;
        }

        public static SearchData CreateData(int data, string name = "Id")
        {
            return new SearchData(new List<Tuple<object, string>> { new Tuple<object, string>(data, name) }, data);
        }
        public static SearchData CreateData2(int data, long data2, string name = "Id", string name2 = "valCount")
        {
            return new SearchData(new List<Tuple<object, string>> { new Tuple<object, string>(data, name), new Tuple<object, string>(data2, name2) }, data);
        }

        public static string Quote(this string str)
        {
            return "\"" + str + "\"";
        }
    }
}

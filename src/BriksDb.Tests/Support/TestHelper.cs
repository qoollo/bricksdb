using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Data.TransactionTypes;
using Qoollo.Impl.Common.HashHelp;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;
using Qoollo.Impl.NetInterfaces.Distributor;
using Qoollo.Impl.NetInterfaces.Writer;
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
            var host = new ServiceHost(ret,
                                       new Uri(string.Format("net.tcp://{0}:{1}/{2}", server.RemoteHost, server.Port,
                                                             config.ServiceName)));
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

            return ret;
        }

        public static TestNetDistributorForProxy OpenDistributorHostForDb(ServerId server, ConnectionConfiguration config)
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

            return ret;
        }

        public static SearchData CreateData(int data)
        {
            return new SearchData(new List<Tuple<object, string>> { new Tuple<object, string>(data, "") }, data);
        }
    }
}

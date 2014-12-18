using System;
using System.ServiceModel;
using System.ServiceModel.Description;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Configurations;

namespace Qoollo.Impl.NetInterfaces.Data
{
    internal static class NetConnector
    {
        public static ChannelFactory<TApi> Connect<TApi>(ServerId server, string serviceName,
            ConnectionTimeoutConfiguration timeoutConfiguration)
        {
            string endPointAddr = string.Format("net.tcp://{0}:{1}/{2}", server.RemoteHost, server.Port, serviceName);
            var endpointAddress =
                new EndpointAddress(endPointAddr);

            var tcpBinding = new NetTcpBinding
            {
                Security = {Mode = SecurityMode.None},
                MaxReceivedMessageSize = 2147483647,
                MaxBufferSize = 2147483647,
                OpenTimeout = timeoutConfiguration.OpenTimeout,
                SendTimeout = timeoutConfiguration.SendTimeout,
            };
            try
            {
                var cf = new ChannelFactory<TApi>(tcpBinding, endpointAddress);
                foreach (OperationDescription op in cf.Endpoint.Contract.Operations)
                {
                    var dataContractBehavior = op.Behaviors.Find<DataContractSerializerOperationBehavior>();
                    if (dataContractBehavior != null)
                    {
                        dataContractBehavior.MaxItemsInObjectGraph = int.MaxValue;
                    }
                }
                return cf;
            }
            catch (Exception e)
            {
                Logger.Logger.Instance.Error(e, "");
                return null;
            }

        }

        public static void CreateServer<T>(object server, NetReceiverConfiguration configuration)
        {
            var host = new ServiceHost(server,
                new Uri(string.Format("net.tcp://{0}:{1}/{2}", configuration.Host, configuration.Port,
                    configuration.Service)));

            var binding = new NetTcpBinding()
            {
                Security = { Mode = SecurityMode.None },
                MaxBufferSize = 2147483647,
                MaxReceivedMessageSize = 2147483647
            };
            var contractType = typeof(T);
            host.AddServiceEndpoint(contractType, binding, "");

            var behavior = host.Description.Behaviors.Find<ServiceBehaviorAttribute>();
            behavior.InstanceContextMode = InstanceContextMode.Single;
            behavior.ConcurrencyMode = ConcurrencyMode.Multiple;
            behavior.MaxItemsInObjectGraph = 2147483647;
            behavior.ReleaseServiceInstanceOnTransactionComplete = false;

            var test = host.Description.Behaviors.Find<ServiceDebugBehavior>();
            test.IncludeExceptionDetailInFaults = true;

            host.Open();
        }
    }
}

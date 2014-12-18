using System.ServiceModel;
using Qoollo.Impl.Common;

namespace Qoollo.Impl.NetInterfaces
{
    [ServiceContract]
    internal interface ICommonCommunicationNet
    {
        [OperationContract]
        RemoteResult SendSync(NetCommand command);

        [OperationContract(IsOneWay = true)]
        void SendASync(NetCommand command);

        [OperationContract]
        RemoteResult Ping();
    }
}

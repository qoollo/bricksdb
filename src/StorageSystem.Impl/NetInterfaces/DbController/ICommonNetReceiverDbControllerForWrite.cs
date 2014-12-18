using System.ServiceModel;

namespace Qoollo.Impl.NetInterfaces.DbController
{
    [ServiceContract]
    internal interface ICommonNetReceiverDbControllerForWrite : IRemoteNet, ICommonCommunicationNet
    {
    }
}

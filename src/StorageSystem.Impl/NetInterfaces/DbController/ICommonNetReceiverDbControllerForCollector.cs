using System.ServiceModel;

namespace Qoollo.Impl.NetInterfaces.DbController
{
    [ServiceContract]
    internal interface ICommonNetReceiverDbControllerForCollector:ICommonCommunicationNet, ISearchNet
    {
    }
}

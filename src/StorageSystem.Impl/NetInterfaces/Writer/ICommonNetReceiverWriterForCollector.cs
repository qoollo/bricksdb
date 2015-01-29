using System.ServiceModel;

namespace Qoollo.Impl.NetInterfaces.Writer
{
    [ServiceContract]
    internal interface ICommonNetReceiverWriterForCollector:ICommonCommunicationNet, ISearchNet
    {
    }
}

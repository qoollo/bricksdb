using System.ServiceModel;

namespace Qoollo.Impl.NetInterfaces.Writer
{
    [ServiceContract]
    internal interface ICommonNetReceiverWriterForWrite : IRemoteNet, ICommonCommunicationNet
    {
    }
}

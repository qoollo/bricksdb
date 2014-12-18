using System.ServiceModel;

namespace Qoollo.Impl.NetInterfaces.Distributor
{
    [ServiceContract]
    internal interface ICommonNetReceiverForDb : ITransactionAnswer, ICommonCommunicationNet
    {
    }
}

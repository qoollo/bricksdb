using System.ServiceModel;
using Qoollo.Impl.Common.Data.DataTypes;
using Qoollo.Impl.Common.Data.TransactionTypes;

namespace Qoollo.Impl.NetInterfaces.Distributor
{
    [ServiceContract]
    internal interface IProxyRemoteNet
    {
        [OperationContract(IsOneWay = true)]
        void Process(InnerData ev);
        [OperationContract]
        UserTransaction GetTransaction(UserTransaction transaction);
    }
}

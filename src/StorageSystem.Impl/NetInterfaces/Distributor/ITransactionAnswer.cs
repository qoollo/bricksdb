using System.ServiceModel;
using Qoollo.Impl.Common.Data.TransactionTypes;

namespace Qoollo.Impl.NetInterfaces.Distributor
{
    [ServiceContract]
    internal interface ITransactionAnswer
    {
        //[OperationContract(IsOneWay = true)]
        [OperationContract]
        void TransactionAnswer(Transaction transaction);
    }
}

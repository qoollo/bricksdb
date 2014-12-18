using System.Runtime.Serialization;

namespace Qoollo.Impl.Common.NetResults.System.Distributor
{
    [DataContract]
    internal class OperationCompleteCommand:NetCommand
    {
        [DataMember]
        public Common.Data.TransactionTypes.Transaction Transaction { get; private set; }

        public OperationCompleteCommand(Common.Data.TransactionTypes.Transaction transaction)
        {
            Transaction = transaction;
        }
    }
}

using System.ServiceModel;
using Qoollo.Impl.Common;
using Qoollo.Impl.Common.Data.DataTypes;

namespace Qoollo.Impl.NetInterfaces.Writer
{
    [ServiceContract]
    internal interface IRemoteNet
    {
        [OperationContract(IsOneWay = true)]
        void Process(InnerData data);
        
        [OperationContract]
        RemoteResult ProcessSync(InnerData data);

        [OperationContract(IsOneWay = true)]
        void Rollback(InnerData data);

        [OperationContract]
        InnerData ReadOperation(InnerData data);
    }
}

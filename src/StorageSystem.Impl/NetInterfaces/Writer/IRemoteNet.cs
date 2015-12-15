using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
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
        Task<RemoteResult> ProcessTaskBased(InnerData data);

        
        [OperationContract]
        RemoteResult ProcessSync(InnerData data);

        [OperationContract]
        RemoteResult ProcessSync(List<InnerData> datas);

        [OperationContract(IsOneWay = true)]
        void Rollback(InnerData data);

        [OperationContract]
        InnerData ReadOperation(InnerData data);
    }
}

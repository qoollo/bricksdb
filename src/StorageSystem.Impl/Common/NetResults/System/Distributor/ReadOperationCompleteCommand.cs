using System.Runtime.Serialization;
using Qoollo.Impl.Common.Data.DataTypes;

namespace Qoollo.Impl.Common.NetResults.System.Distributor
{
    [DataContract]
    internal class ReadOperationCompleteCommand:NetCommand
    {
        [DataMember]
        public InnerData Data { get; private set; }

        public ReadOperationCompleteCommand(InnerData data)
        {
            Data = data;
        }
    }
}

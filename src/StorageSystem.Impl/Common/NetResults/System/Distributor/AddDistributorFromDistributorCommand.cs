using System.Runtime.Serialization;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Common.NetResults.System.Distributor
{
    [DataContract]
    internal class AddDistributorFromDistributorCommand:NetCommand
    {
        [DataMember]
        public ServerId Server { get; private set; }

        public AddDistributorFromDistributorCommand(ServerId server)
        {
            Server = server;
        }
    }
}

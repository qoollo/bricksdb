using System.Collections.Generic;
using System.Runtime.Serialization;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Common.NetResults.System.Writer
{    
    [DataContract]
    internal class RestoreCommand:NetCommand
    {
        [DataMember]
        public ServerId RestoreServer { get; private set; }
        [DataMember]
        public RestoreState RestoreState { get; private set; }
        [DataMember]
        public string TableName { get; set; }        
        public List<ServerId> FailedServers { get; set; } 

        public RestoreCommand(ServerId server, string tableName, RestoreState state)
        {
            RestoreServer = server;
            TableName = tableName;
            RestoreState = state;
        }
    }
}

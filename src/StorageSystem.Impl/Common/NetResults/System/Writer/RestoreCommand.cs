using System.Collections.Generic;
using System.Runtime.Serialization;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Common.NetResults.System.Writer
{
    [DataContract]
    internal class RestoreCommand:NetCommand
    {
        [DataMember]
        public ServerId RestoreServer { get; private set; }

        [DataMember]
        public bool IsModelUpdated { get; private set; }
        
        [DataMember]
        public string TableName { get; set; }

        public List<ServerId> FailedServers { get; set; } 

        public RestoreCommand(ServerId server, bool isModelUpdated, string tableName)
        {
            RestoreServer = server;
            IsModelUpdated = isModelUpdated;
            TableName = tableName;
        }
    }
}

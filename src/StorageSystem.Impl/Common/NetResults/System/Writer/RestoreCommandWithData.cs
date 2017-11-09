using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Common.NetResults.System.Writer
{
    [DataContract]
    internal class RestoreCommandWithData:NetCommand
    {
        [DataMember]
        public ServerId ServerId { get; private set; }

        [DataMember]
        public RestoreState RestoreState { get; private set; }

        [DataMember]
        public string TableName { get; private set; }

        public RestoreCommandWithData(ServerId serverId, string tableName, RestoreState state)
        {
            TableName = tableName;
            ServerId = new ServerId(serverId);
            RestoreState = state;
        }

        protected RestoreCommandWithData(string tableName)
        {
            TableName = tableName;
        }
    }
}

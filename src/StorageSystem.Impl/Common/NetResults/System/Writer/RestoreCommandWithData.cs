using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Qoollo.Impl.Common.HashFile;
using Qoollo.Impl.Common.Server;

namespace Qoollo.Impl.Common.NetResults.System.Writer
{
    [DataContract]
    internal class RestoreCommandWithData:NetCommand
    {
        [DataMember]
        public ServerId ServerId { get; private set; }

        [DataMember]
        public List<KeyValuePair<string, string>> Hash { get; private set; }

        [DataMember]
        public bool IsModelUpdated { get; private set; }

        [DataMember]
        public string TableName { get; private set; }

        public RestoreCommandWithData(ServerId serverId, List<HashMapRecord> hash, bool isModelUpdated, string tableName)
        {
            TableName = tableName;
            ServerId = new ServerId(serverId);
            Hash = hash.Select(x => new KeyValuePair<string, string>(x.Begin, x.End)).ToList();
            IsModelUpdated = isModelUpdated;
        }

        protected RestoreCommandWithData(string tableName)
        {
            TableName = tableName;
        }
    }
}

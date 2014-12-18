using System;
using System.Runtime.Serialization;

namespace Qoollo.Impl.Common.Data.DataTypes
{
    [DataContract]
    public class MetaData
    {
        public MetaData(bool isLocal, DateTime deleteTime, bool isDeleted)
        {
            IsDeleted = isDeleted;
            DeleteTime = deleteTime;
            IsLocal = isLocal;
        }

        [DataMember]
        public bool IsLocal { get; private set; }
        [DataMember]
        public DateTime DeleteTime { get; private set; }
        [DataMember]
        public bool IsDeleted { get; private set; }
        
        public object Id { get; set; }
        public string Hash { get; set; }
    }
}

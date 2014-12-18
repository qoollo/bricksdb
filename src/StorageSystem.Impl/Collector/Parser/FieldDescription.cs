using System;
using System.Runtime.Serialization;

namespace Qoollo.Impl.Collector.Parser
{
    [DataContract]
    public class FieldDescription
    {
        public FieldDescription(string fieldName, Type systemFieldType)
        {
            SystemFieldType = systemFieldType;
            FieldName = fieldName;
            AsFieldName = FieldName;
        }

        public FieldDescription(string fieldName, int userType, object value)
        {
            UserType = userType;
            Value = value;
            FieldName = fieldName;
            AsFieldName = FieldName;
        }

        [DataMember]
        public string FieldName { get; private set; }
        [DataMember]
        public string AsFieldName { get; set; }

        public Type SystemFieldType { get; private set; }
        [DataMember]
        public object Value { get; set; }
        [DataMember]
        public int PageSize { get; set; }
        [DataMember]
        public bool IsFirstAsk { get; set; }
        [DataMember]        
        public int UserType { get; set; }
    }
}

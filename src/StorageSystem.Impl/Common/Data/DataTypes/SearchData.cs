using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Qoollo.Impl.Common.Data.DataTypes
{
    [DataContract]
    public class SearchData
    {
        public SearchData(List<Tuple<object, string>> fields, object key)
        {
            Key = key;
            Fields = fields;
        }

        [DataMember]
        public List<Tuple<object, string>> Fields { get; private set; }
        [DataMember]
        public object Key { get; private set; }
    }
}

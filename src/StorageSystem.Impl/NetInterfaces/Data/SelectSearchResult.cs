using System.Collections.Generic;
using System.Runtime.Serialization;
using Qoollo.Impl.Common.Data.DataTypes;

namespace Qoollo.Impl.NetInterfaces.Data
{
    [DataContract]
    public class SelectSearchResult
    {
        public SelectSearchResult(List<SearchData> data, bool isAllDataRead)
        {
            IsAllDataRead = isAllDataRead;
            Data = data;
        }

        [DataMember]
        public List<SearchData> Data { get; private set; }
        [DataMember]
        public bool IsAllDataRead { get; private set; }
    }
}

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Qoollo.Impl.Collector.Parser
{
    [DataContract]
    public class SelectDescription
    {
        public SelectDescription(FieldDescription idDescription, string script, int countElements, List<FieldDescription> userParametrs)
        {
            UserParametrs = userParametrs;
            CountElements = countElements;
            Script = script;
            IdDescription = idDescription;
        }

        [DataMember]
        public FieldDescription IdDescription { get; private set; }
        [DataMember]
        public string Script { get; private set; }
        [DataMember]
        public int CountElements { get; set; }
        [DataMember]
        public List<FieldDescription> UserParametrs { get; private set; }
        [DataMember]
        public string TableName { get; set; }
    }
}

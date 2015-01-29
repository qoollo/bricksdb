using System.Collections.Generic;
using System.Runtime.Serialization;
using Qoollo.Impl.Collector.Parser;

namespace Qoollo.Impl.Writer.Db
{
    internal class SelectDescriptionForGeneric<TCommand>
    {
        public SelectDescriptionForGeneric(FieldDescription idDescription, TCommand script, int countElements, List<FieldDescription> userParametrs)
        {
            UserParametrs = userParametrs;
            CountElements = countElements;
            Script = script;
            IdDescription = idDescription;
        }

        [DataMember]
        public FieldDescription IdDescription { get; private set; }
        [DataMember]
        public TCommand Script { get; private set; }
        [DataMember]
        public int CountElements { get; set; }
        [DataMember]
        public List<FieldDescription> UserParametrs { get; private set; }
        [DataMember]
        public string TableName { get; set; }
    }
}

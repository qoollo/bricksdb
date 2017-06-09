using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common.Data.Support;

namespace Qoollo.Tests.TestCollector
{
    class TestIntParser : ScriptParser
    {
        public override ScriptType ParseQueryType(string script)
        {
            if (script.StartsWith("asc"))
                return ScriptType.OrderAsc;
            if (script.StartsWith("desc"))
                return ScriptType.OrderDesc;

            return ScriptType.OrderDesc;
        }

        public override Tuple<FieldDescription, string> PrepareOrderScript(string script, int pageSize, IUserCommandsHandler handler)
        {
            if (script == "asc2calc" || script == "desc2calc")
                return new Tuple<FieldDescription, string>(new FieldDescription("Id", typeof(int)) { ContainsCalculatedField = true }, script);

            return new Tuple<FieldDescription, string>(new FieldDescription("Id", typeof(int)), script);
        }

        public override Tuple<FieldDescription, string> PrepareKeyScript(string script, IUserCommandsHandler handler)
        {
            return new Tuple<FieldDescription, string>(new FieldDescription("Id", typeof(int)), script);
        }

        public override List<FieldDescription> GetOrderKeys(string script, IUserCommandsHandler handler)
        {
            if (script == "asc2calc" || script == "desc2calc")
            {
                return new List<FieldDescription>()
                {
                    new FieldDescription("valCount", typeof(long)),
                    new FieldDescription("id", typeof(int)),                 
                };
            }

            return new List<FieldDescription>() { new FieldDescription("id", typeof(int)) };
        }
    }
}

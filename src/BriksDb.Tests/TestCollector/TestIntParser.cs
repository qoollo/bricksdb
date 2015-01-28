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
            if (script == "asc")
                return ScriptType.OrderAsc;
            if (script == "desc")
                return ScriptType.OrderDesc;

            return ScriptType.OrderDesc;
        }

        public override Tuple<FieldDescription, string> PrepareOrderScript(string script, int pageSize, IUserCommandsHandler handler)
        {
            return new Tuple<FieldDescription, string>(new FieldDescription("", typeof(int)), script);
        }

        public override Tuple<FieldDescription, string> PrepareKeyScript(string script, IUserCommandsHandler handler)
        {
            return new Tuple<FieldDescription, string>(new FieldDescription("", typeof(int)), script);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Impl.Postgre
{
    public class PostgreScriptParsingException : Exception
    {
        public PostgreScriptParsingException()
        {
        }

        public PostgreScriptParsingException(string message) : base(message)
        {
        }

        public PostgreScriptParsingException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected PostgreScriptParsingException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}

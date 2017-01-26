using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Impl.Postgre.Internal
{
    internal static class PostgreHelper
    {
        public static bool IsQuoted(this string str)
        {
            return str.Length >= 3 && str[0] == '\"' && str[str.Length - 1] == '\"';
        }
        public static string Quote(this string str)
        {
            return "\"" + str + "\"";
        }
        public static string UnQuote(this string str)
        {
            if (!IsQuoted(str))
                return str;

            return str.Substring(1, str.Length - 1);
        }
    }
}

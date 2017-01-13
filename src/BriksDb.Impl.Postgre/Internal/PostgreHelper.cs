using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Impl.Postgre.Internal
{
    internal static class PostgreHelper
    {
        public static string Quote(this string str)
        {
            return "\"" + str + "\"";
        }
    }
}

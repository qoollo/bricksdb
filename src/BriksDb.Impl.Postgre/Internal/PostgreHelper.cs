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

            return str.Substring(1, str.Length - 2);
        }

        public static string NormalizeName(string name)
        {
            if (IsQuoted(name))
                return name.Substring(1, name.Length - 2);
            return name.ToLower();
        }

        private static bool AreNamesEqual(string name1, string name2)
        {
            bool quoted1 = IsQuoted(name1);
            bool quoted2 = IsQuoted(name2);

            if (!quoted1 && !quoted2)
                return string.Equals(name1, name2, StringComparison.OrdinalIgnoreCase);

            if (quoted1 && quoted2)
            {
                if (name1.Length != name2.Length)
                    return false;
                return string.Compare(name1, 1, name2, 1, name1.Length - 2) == 0;
            }

            if (quoted1 && name1.Length - 2 != name2.Length)
                return false;
            if (quoted2 && name1.Length != name2.Length - 2)
                return false;

            return NormalizeName(name1) == NormalizeName(name2);
        }
        public static bool AreNamesEqual(string name1, bool n1Normed, string name2, bool n2Normed)
        {
            if (n1Normed && n2Normed)
                return name1 == name2;

            if (!n1Normed && !n2Normed)
                return AreNamesEqual(name1, name2);

            if (n1Normed)
            {
                bool quoted2 = IsQuoted(name2);
                if (quoted2 && name1.Length != name2.Length - 2)
                    return false;
                if (!quoted2 && name1.Length != name2.Length)
                    return false;

                return name1 == NormalizeName(name2);
            }

            if (n2Normed)
            {
                bool quoted1 = IsQuoted(name1);
                if (quoted1 && name1.Length - 2 != name2.Length)
                    return false;
                if (!quoted1 && name1.Length != name2.Length)
                    return false;

                return NormalizeName(name1) == name2;
            }

            return false;
        }
    }
}

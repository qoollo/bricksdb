using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Postgre.Internal
{
    internal class PostgreScriptParser : ScriptParser
    {
        #region ============ Common helpers ===========
        internal static int IndexOfWholePhrase(string str, string word, int startIndex = 0, bool ignoreCase = true)
        {
            if (startIndex < 0)
                startIndex = 0;

            if (word.IndexOf(' ') > 0)
            {
                // use regexp for spaces
                string input = startIndex <= 0 ? str : str.Substring(startIndex);
                string pattern = @"(?:^|\W)" + word.Replace(" ", @"\s+") + @"(?:$|\W)";
                RegexOptions options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;

                var match = Regex.Match(input, pattern, options);
                if (!match.Success)
                    return -1;
                if (match.Index == 0)
                    return startIndex;
                return match.Index + startIndex + 1;
            }
            else
            {
                StringComparison comparsionType = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

                int index = startIndex - 1;
                while (true)
                {
                    index = str.IndexOf(word, index + 1, comparsionType);
                    if (index < 0)
                        return -1;

                    if (index == 0 || !char.IsLetterOrDigit(str[index - 1]))
                        if (index + word.Length >= str.Length || !char.IsLetterOrDigit(str[index + word.Length]))
                            return index;
                }
            }
        }

        internal static int LastIndexOfWholePhrase(string str, string word, int startIndex = -1, bool ignoreCase = true)
        {
            if (startIndex < 0)
                startIndex = str.Length - 1;

            if (word.IndexOf(' ') > 0)
            {
                // use regexp for spaces
                string input = startIndex >= str.Length - 1 ? str : str.Substring(0, startIndex + 1);
                string pattern = @"(?:^|\W)" + word.Replace(" ", @"\s+") + @"(?:$|\W)";
                RegexOptions options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
                options |= RegexOptions.RightToLeft;
                var match = Regex.Match(input, pattern, options);
                if (!match.Success)
                    return -1;
                if (match.Index == 0)
                    return 0;
                return match.Index + 1;
            }
            else
            {
                StringComparison comparsionType = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

                int index = startIndex + 1;
                while (true)
                {
                    index = str.LastIndexOf(word, index - 1, comparsionType);
                    if (index < 0)
                        return -1;

                    if (index == 0 || !char.IsLetterOrDigit(str[index - 1]))
                        if (index + word.Length >= str.Length || !char.IsLetterOrDigit(str[index + word.Length]))
                            return index;
                }
            }
        }
        #endregion

        #region ========= ORDER BY helpers =============

        internal class OrderByInfo
        {
            public static OrderByInfo Create(string script)
            {
                OrderByInfo result = new OrderByInfo() { Script = script };
                var match = Regex.Match(script, PostgreConsts.OrderByRegEx, RegexOptions.IgnoreCase);
                if (!match.Success)
                    return result;

                // Look for last match
                while (true)
                {
                    var nextMatch = match.NextMatch();
                    if (nextMatch == null || !nextMatch.Success)
                        break;
                    match = nextMatch;
                }

                // Order by should be after FROM
                int indexOfFrom = script.IndexOf(PostgreConsts.From, StringComparison.OrdinalIgnoreCase);
                if (indexOfFrom >= 0 && match.Index < indexOfFrom)
                    return result;

                result.OrderByStart = match.Groups[0].Index;
                result.OrderByLength = match.Groups[0].Length;
                result.OrderKeysStart = match.Groups[2].Index;
                result.OrderKeysLength = match.Groups[2].Length;
                result.OrderType = ScriptType.OrderAsc;
                if (match.Groups[3].Success)
                    if (string.Equals(match.Groups[3].Value, PostgreConsts.Desc, StringComparison.OrdinalIgnoreCase))
                        result.OrderType = ScriptType.OrderDesc;

                return result;
            }

            public string Script { get; private set; }
            public int OrderByStart { get; private set; }
            public int OrderByLength { get; private set; }
            public ScriptType OrderType { get; private set; }
            public bool IsOrdered { get { return OrderType != ScriptType.Unknown; } }
            public int OrderKeysStart { get; private set; }
            public int OrderKeysLength { get; private set; }

            public string OrderByClause => IsOrdered ? Script.Substring(OrderByStart, OrderByLength) : null;
            public string KeysSubstring => IsOrdered ? Script.Substring(OrderKeysStart, OrderKeysLength) : null;
        }


        private static OrderByInfo GetOrderByInfo(string script)
        {
            return OrderByInfo.Create(script);
        }


        #endregion

        #region Public

        public override ScriptType ParseQueryType(string script)
        {
            var orderByInfo = GetOrderByInfo(script);
            return orderByInfo.OrderType;
        }

        public override Tuple<FieldDescription, string> PrepareOrderScript(string script, int pageSize, IUserCommandsHandler handler)
        {
            throw new NotImplementedException();
            //var query = script.ToLower();

            //var key = FindOrderKey(query, handler);
            //if (key.Equals(default(KeyValuePair<string, Type>)))
            //    return null;

            //var field = new FieldDescription(key.Key, key.Value);
            //field.AsFieldName = GetAsName(query, key.Key.ToLower(), ref query);

            //return new Tuple<FieldDescription, string>(field, query);
        }

        public override Tuple<FieldDescription, string> PrepareKeyScript(string script, IUserCommandsHandler handler)
        {
            throw new NotImplementedException();
            //var query = script.ToLower();

            //var key = handler.GetKeyName();

            //var fields = GetSelectFields(query, handler);
            //if (fields == null || fields.Count == 0)
            //    return null;

            //FieldDescription description = null;
            //if (fields[0] == SqlConsts.All)
            //{
            //    description = new FieldDescription(key, handler.GetDbFieldsDescription().First(x => x.Item1 == key).Item2);

            //    return new Tuple<FieldDescription, string>(description, query);
            //}

            //int pos = fields.FindIndex(x => x == key);
            //if (pos == -1)
            //{
            //    query = AddAfter(query, fields.Last(), "," + key);
            //}

            //description = new FieldDescription(key, handler.GetDbFieldsDescription().First(x => x.Item1 == key).Item2);
            //return new Tuple<FieldDescription, string>(description, query);
        }

        #endregion

        #region Private
/*
        private KeyValuePair<string, Type> FindOrderKey(string query, IUserCommandsHandler handler)
        {
            var split = Regex.Split(query, SqlConsts.OrderBy);

            if (split.Length < 2)
                return default(KeyValuePair<string, Type>);

            var order = split[1].Split(new[] { ' ', ',' }).ToList();
            order.RemoveAll(x => x == "");

            var key = order.FirstOrDefault();

            if (key == null)
                return default(KeyValuePair<string, Type>);

            //TODO fix
            key = key.Replace('(', ' ').Replace(')', ' ').Trim();

            if (key != null)
            {
                var ere = handler.GetDbFieldsDescription();
                var field = handler.GetDbFieldsDescription().FirstOrDefault(x => x.Item1.ToLower() == key);
                if (field != null)
                    return new KeyValuePair<string, Type>(key, field.Item2);
            }

            return default(KeyValuePair<string, Type>);
        }

        private List<string> GetSelectFields(string query, IUserCommandsHandler handler)
        {
            var ret = new List<string>();
            string selectSplitPattern = "";

            int from = query.IndexOf(SqlConsts.From, System.StringComparison.Ordinal);
            if (from != -1)
                selectSplitPattern = SqlConsts.From;

            if (selectSplitPattern == "")
            {
                int where = query.IndexOf(SqlConsts.Where, System.StringComparison.Ordinal);
                if (where != -1)
                    selectSplitPattern = SqlConsts.Where;
            }

            if (selectSplitPattern == "")
            {
                int orderBy = query.IndexOf(SqlConsts.OrderBy, System.StringComparison.Ordinal);
                if (orderBy != -1)
                    selectSplitPattern = SqlConsts.OrderBy;
            }

            if (selectSplitPattern == "")
                return null;

            var split = Regex.Split(query, selectSplitPattern).ToList();
            split.RemoveAll(x => x == "");

            if (split.Count < 2)
                return null;

            string select = split.First();

            if (select.Contains(SqlConsts.All))
            {
                ret.Add(SqlConsts.All);
            }
            else
            {
                ret.AddRange(select.Split(new[] { ' ', ',' }));
                ret.RemoveAll(x => x == "" || x == "*");
                ret.RemoveAt(0);

                if (query.Contains("top"))
                {
                    ret.RemoveAt(0);
                    ret.RemoveAt(0);
                }

                for (int i = ret.Count - 1; i >= 0; i++)
                {
                    if (ret[i] == "as")
                    {
                        ret.RemoveAt(i);
                        ret.RemoveAt(i);
                    }
                }

                ret.RemoveAll(x => handler.GetDbFieldsDescription().FirstOrDefault(y => y.Item1 == x) == null);
                if (ret.Count == 0)
                    return null;
            }
            return ret;
        }

        private string AddAfter(string query, string pos, string value)
        {
            return query.Insert(query.IndexOf(pos, System.StringComparison.Ordinal) + pos.Length, value);
        }

        private string AddBefore(string query, string pos, string value)
        {
            return query.Insert(query.IndexOf(pos, System.StringComparison.Ordinal), value);
        }

        public string AddPageToSelect(string query)
        {
            if (!query.Contains("top"))
                query = AddAfter(query, SqlConsts.Select, " top @" + Consts.Page);

            return query;
        }
        */
        #endregion

        #region New Select
            /*
        private string GetAsName(string query, string key, ref string rquery)
        {
            string selectSplitPattern = "";

            query = query.Replace(SqlConsts.Select, "");

            int from = query.IndexOf(SqlConsts.From, System.StringComparison.Ordinal);
            if (from != -1)
                selectSplitPattern = SqlConsts.From;

            if (selectSplitPattern == "")
            {
                int where = query.IndexOf(SqlConsts.Where, System.StringComparison.Ordinal);
                if (where != -1)
                    selectSplitPattern = SqlConsts.Where;
            }

            if (selectSplitPattern == "")
                return key;

            var split = Regex.Split(query, selectSplitPattern).ToList();
            split.RemoveAll(x => x == "");

            string old = split[0];

            split = split[0].Split(',').ToList();

            bool find = false;
            foreach (var param in split)
            {
                string str = param.Trim();

                var sp = str.Split(' ').ToList();
                if (sp.Count == 3 && sp[1] == SqlConsts.As && (sp[0] == key || sp[2].Replace("'", "").Trim() == key))
                {
                    return sp.Last().Replace("'", "");
                }

                if (sp.Count != 0 && sp[0] == key)
                {
                    find = true;
                }
            }

            if (!find && !old.Contains("*"))
            {
                rquery = AddBefore(rquery, selectSplitPattern, " , " + key + " ");
            }

            return key;
        }
        */

        public string CutOrderby(ref string script)
        {
            throw new NotImplementedException();
            //if (script.IndexOf(SqlConsts.From, System.StringComparison.Ordinal) >
            //    script.IndexOf(SqlConsts.OrderBy, System.StringComparison.Ordinal))
            //    return script;

            //int pos = script.IndexOf(SqlConsts.OrderBy, System.StringComparison.Ordinal);
            //var order = script.Substring(pos, script.Length - pos);
            //script = script.Remove(pos);

            //return order;
        }

        public string CreateOrderScript(string script, FieldDescription idDescription)
        {
            return script;

            //var type = ParseQueryType(script);
            //CutOrderby(ref script);

            //switch (type)
            //{
            //    case ScriptType.OrderAsc:
            //        return OrderAsc(script, idDescription);

            //    case ScriptType.OrderDesc:
            //        return OrderDesc(script, idDescription);
            //}
            //return "";
        }
        /*
        private static string CutDeclare(ref string script)
        {
            if (script.Contains(SqlConsts.Declare))
            {
                int begin = script.IndexOf(SqlConsts.Declare, System.StringComparison.Ordinal);
                int end = script.IndexOf(script.Contains(SqlConsts.With) ? SqlConsts.With : SqlConsts.Select,
                    System.StringComparison.Ordinal);

                string declare = script.Substring(begin, end - begin);
                script = script.Replace(declare, " ");
                return declare;
            }

            return "";
        }

        private static bool WithExists(string script)
        {
            return script.Contains(SqlConsts.With);
        }

        private static string CutSelectWhenWith(string script)
        {
            int index = FindLastIndex(script, SqlConsts.Select);

            return script.Remove(index);
        }

        private static int FindLastIndex(string str, string template)
        {
            int index = str.Length;

            while (index != -1)
            {
                index = str.LastIndexOf(template, index - 1, System.StringComparison.Ordinal);

                if ((index == 0 || !char.IsLetterOrDigit(str[index - 1])) &&
                    (index + template.Length == str.Length ||
                     !char.IsLetterOrDigit(str[index + template.Length])))
                {
                    return index;
                }
            }
            return -1;
        }

        private static string GetTableNameWhenWith(string script)
        {
            var split = script.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            return split[1].Trim();
        }
        */
        #endregion

        #region Order
            /*
        private string OrderAsc(string script, FieldDescription idDescription)
        {
            string declare = CutDeclare(ref script);

            var nquery = SqlConsts.Select + " * ";

            nquery = AddPageToSelect(nquery);
            nquery = nquery.Replace("@" + Consts.Page, (idDescription.PageSize).ToString());

            if (!WithExists(script))
                return
                        string.Format("{0} ; {1} from ( {2} ) as HelpTable " +
                                      " where HelpTable.{3} {4} @{5} order by HelpTable.{3}",
                            declare, nquery, script, idDescription.AsFieldName,
                            idDescription.IsFirstAsk ? ">=" : ">", idDescription.FieldName);

            string withName = GetTableNameWhenWith(script);
            script = CutSelectWhenWith(script);

            return
                string.Format("{0} ; {2} {1} from {6} as HelpTable " +
                              " where HelpTable.{3} {4} @{5} order by HelpTable.{3}",
                    declare, nquery, script, idDescription.AsFieldName,
                    idDescription.IsFirstAsk ? ">=" : ">", idDescription.FieldName, withName);
        }

        private string OrderDesc(string script, FieldDescription idDescription)
        {
            string declare = CutDeclare(ref script);
            var nquery = SqlConsts.Select + " * ";

            nquery = AddPageToSelect(nquery);
            nquery = nquery.Replace("@" + Consts.Page, (idDescription.PageSize).ToString());

            if (!WithExists(script))
            {
                if (idDescription.IsFirstAsk)
                    nquery =
                            string.Format("{0} ; {1} from ( {3} ) as HelpTable order by HelpTable.{2} desc",
                                declare, nquery, idDescription.AsFieldName, script);
                else
                    nquery =
                            string.Format("{0} ; {1} from ( {3} ) as HelpTable " +
                                          " where  HelpTable.{2} < @{4} order by HelpTable.{2} desc ",
                                declare, nquery, idDescription.AsFieldName, script, idDescription.FieldName);

                return nquery;
            }

            string withName = GetTableNameWhenWith(script);
            script = CutSelectWhenWith(script);

            if (idDescription.IsFirstAsk)
                nquery =
                        string.Format("{0} ; {3} {1} from {4} as HelpTable order by HelpTable.{2} desc",
                            declare, nquery, idDescription.AsFieldName, script, withName);
            else
                nquery = string.Format("{0} ; {3} {1} from {5} as HelpTable " +
                                      " where HelpTable.{2} < @{4} order by HelpTable.{2} desc",
                            declare, nquery, idDescription.AsFieldName, script, idDescription.FieldName, withName);

            return nquery;
        }
        */
        #endregion

        //option(Recompile)
    }
}
 
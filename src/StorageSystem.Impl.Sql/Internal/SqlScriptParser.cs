using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Support;

namespace Qoollo.Impl.Sql.Internal
{
    internal class SqlScriptParser:ScriptParser
    {
        #region Public

        public override ScriptType ParseQueryType(string script)
        {
            var query = script.ToLower();

            if (query.Contains(SqlConsts.OrderBy))
            {
                if (query.Contains(SqlConsts.Asc))
                    return ScriptType.OrderAsc;
                if (query.Contains(SqlConsts.Desc))
                    return ScriptType.OrderDesc;

                return ScriptType.OrderAsc;
            }
            return ScriptType.Unknown;
        }       

        public override Tuple<FieldDescription, string> PrepareOrderScript(string script, int pageSize, IUserCommandsHandler handler)
        {
            var query = script.ToLower();

            var key = FindOrderKey(query, handler);
            if (key.Equals(default(KeyValuePair<string, Type>)))
                return null;

            var field = new FieldDescription(key.Key, key.Value);
            field.AsFieldName = GetAsName(query, key.Key.ToLower(), ref query);

            return new Tuple<FieldDescription, string>(field, query);
        }

        public override Tuple<FieldDescription, string> PrepareKeyScript(string script,  IUserCommandsHandler handler)
        {
            var query = script.ToLower();

            var key = handler.GetKeyName();

            var fields = GetSelectFields(query, handler);
            if (fields == null || fields.Count == 0)
                return null;

            FieldDescription description = null;
            if (fields[0] == SqlConsts.All)
            {
                description = new FieldDescription(key, handler.GetDbFieldsDescription().First(x => x.Item1 == key).Item2);

                return new Tuple<FieldDescription, string>(description, query);
            }

            int pos = fields.FindIndex(x => x == key);
            if (pos == -1)
            {
                query = AddAfter(query, fields.Last(), "," + key);
            }

            description = new FieldDescription(key, handler.GetDbFieldsDescription().First(x => x.Item1 == key).Item2);
            return new Tuple<FieldDescription, string>(description, query);
        }

        #endregion

        #region Private

        private KeyValuePair<string, Type> FindOrderKey(string query, IUserCommandsHandler handler)
        {
            var split = Regex.Split(query, SqlConsts.OrderBy);

            if (split.Length < 2)
                return default(KeyValuePair<string, Type>);

            var order = split[1].Split(new[] { ' ', ',' }).ToList();
            order.RemoveAll(x => x == "");

            var key = order.FirstOrDefault();

            if(key ==null)
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

            var split = Regex.Split(query, selectSplitPattern).ToList() ;
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
                ret.AddRange(select.Split(new[] {' ', ','}));
                ret.RemoveAll(x => x == "" || x== "*");
                ret.RemoveAt(0);
                
                if (query.Contains("top"))
                {
                    ret.RemoveAt(0);
                    ret.RemoveAt(0);
                }

                for (int i = ret.Count-1; i >=0; i++)
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
            return query.Insert(query.IndexOf(pos, System.StringComparison.Ordinal),  value);
        }

        public string AddPageToSelect(string query)
        {
            if (!query.Contains("top"))
                query =  AddAfter(query, SqlConsts.Select, " top @" + Consts.Page);            

            return query;
        }       

        #endregion

        #region New Select

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
                if (sp.Count == 3 && sp[1] == SqlConsts.As && (sp[0] == key || sp[2].Replace("'","").Trim() == key))
                {
                    return sp.Last().Replace("'","");
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

        public string CutOrderby(ref string script)
        {
            if (script.IndexOf(SqlConsts.From, System.StringComparison.Ordinal) >
                script.IndexOf(SqlConsts.OrderBy, System.StringComparison.Ordinal))
                return script;

            int pos = script.IndexOf(SqlConsts.OrderBy, System.StringComparison.Ordinal);
            var order = script.Substring(pos, script.Length - pos);
            script = script.Remove(pos);

            return order;
        }

        public string CreateOrderScript(string script,  FieldDescription idDescription)
        {
            var type = ParseQueryType(script);
            CutOrderby(ref script);

            switch (type)
            {
                case ScriptType.OrderAsc:
                    return OrderAsc(script, idDescription);

                case ScriptType.OrderDesc:
                    return OrderDesc(script, idDescription);
            }
            return "";
        }

        private static string CutDeclare(ref string script)
        {
            if (script.Contains(SqlConsts.Declare))
            {
                int begin = script.IndexOf(SqlConsts.Declare, System.StringComparison.Ordinal);
                int end = script.IndexOf(script.Contains(SqlConsts.With) ? SqlConsts.With : SqlConsts.Select,
                    System.StringComparison.Ordinal);

                string declare = script.Substring(begin, end-begin);
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
            var split = script.Split(new []{' '}, StringSplitOptions.RemoveEmptyEntries);

            return split[1].Trim();
        }

        #endregion

        #region Order

        private string OrderAsc(string script, FieldDescription idDescription)
        {
            string declare = CutDeclare(ref script);

            var nquery = SqlConsts.Select + " * ";

            nquery = AddPageToSelect(nquery);
            nquery = nquery.Replace("@" + Consts.Page, (idDescription.PageSize).ToString());

            if (!WithExists(script))
            {
                return
                    string.Format("{0} ; {1} from ( {2} ) as HelpTable " +
                                  " where HelpTable.{3} {4} @{5} order by HelpTable.{3}   option(Recompile)",
                        declare, nquery, script, idDescription.AsFieldName,
                        idDescription.IsFirstAsk ? ">=" : ">", idDescription.FieldName);
            }

            string withName = GetTableNameWhenWith(script);
            script = CutSelectWhenWith(script);

            return
                string.Format("{0} ; {2} {1} from {6} as HelpTable " +
                              " where HelpTable.{3} {4} @{5} order by HelpTable.{3}   option(Recompile)",
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
                {
                    nquery =
                        string.Format("{0} ; {1} from ( {3} ) as HelpTable order by HelpTable.{2} desc  option(Recompile)",
                            declare, nquery, idDescription.AsFieldName, script);
                }

                else
                {
                    nquery =
                        string.Format("{0} ; {1} from ( {3} ) as HelpTable " +
                                      " where  HelpTable.{2} < @{4} order by HelpTable.{2} desc  option(Recompile)",
                            declare, nquery, idDescription.AsFieldName, script, idDescription.FieldName);
                }

                return nquery;
            }

            string withName = GetTableNameWhenWith(script);
            script = CutSelectWhenWith(script);

            if (idDescription.IsFirstAsk)
            {
                nquery =
                    string.Format("{0} ; {3} {1} from {4} as HelpTable order by HelpTable.{2} desc  option(Recompile)",
                        declare, nquery, idDescription.AsFieldName, script, withName);
            }

            else
            {
                nquery =
                    string.Format("{0} ; {3} {1} from {5} as HelpTable " +
                                  " where HelpTable.{2} < @{4} order by HelpTable.{2} desc  option(Recompile)",
                        declare, nquery, idDescription.AsFieldName, script, idDescription.FieldName, withName);
            }

            return nquery;
        }

        #endregion
    }
}
 
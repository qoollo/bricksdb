using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Support;
using System.Text;
using Qoollo.Impl.Postgre.Internal.ScriptParsing;

namespace Qoollo.Impl.Postgre.Internal
{
    internal class PostgreScriptParser : ScriptParser
    {
        #region Public

        public override ScriptType ParseQueryType(string script)
        {
            var parsedScript = PostgreSelectScript.Parse(script);
            if (parsedScript.OrderBy == null)
                return ScriptType.Unknown;

            switch (parsedScript.OrderBy.Keys[0].OrderType)
            {
                case OrderType.Asc:
                    return ScriptType.OrderAsc;
                case OrderType.Desc:
                    return ScriptType.OrderDesc;
                default:
                    return ScriptType.OrderAsc;
            }
        }

        public override Tuple<FieldDescription, string> PrepareOrderScript(string script, int pageSize, IUserCommandsHandler handler)
        {
            var parsedScript = PostgreSelectScript.Parse(script);
            if (parsedScript.OrderBy == null || parsedScript.OrderBy.Keys.Count != 1)
                return null;

            var containsCalc = parsedScript.OrderBy.Keys.Any(x => x.IsCalculatable);
            // Order By key
            OrderByKeyElement orderByKey = parsedScript.OrderBy.Keys[0];
            string orderByKeyName = orderByKey.GetKeyName();

            // Search for select Key
            SelectKeyElement selectKey = null;
            if (orderByKey.IsTableQualified)
            {
                selectKey = parsedScript.Select.Keys.FirstOrDefault(o => o.IsTableColumn && string.Compare(o.GetTableColumnName(), orderByKeyName, true) == 0);
            }
            else
            {
                selectKey = parsedScript.Select.Keys.FirstOrDefault(o => string.Compare(o.GetKeyName(), orderByKeyName, true) == 0);
                if (selectKey == null)
                    selectKey = parsedScript.Select.Keys.FirstOrDefault(o => o.IsTableColumn && string.Compare(o.GetTableColumnName(), orderByKeyName, true) == 0);
            }

            // Search for Field Description
            Tuple<string, Type> dbFieldDesc = null;
            var allFields = handler.GetDbFieldsDescription();
            if (selectKey != null && selectKey.IsTableColumn)
            {
                var columnName = selectKey.GetTableColumnName();
                dbFieldDesc = allFields.FirstOrDefault(x => string.Compare(x.Item1, columnName, true) == 0);
            }
            else
            {
                dbFieldDesc = allFields.FirstOrDefault(x => string.Compare(x.Item1, orderByKeyName, true) == 0);
            }

            // Db field not found
            if (dbFieldDesc == null)
                return null;

            // Select key is presented
            if (selectKey != null)
            {
                var resultField = new FieldDescription(orderByKeyName, dbFieldDesc.Item2)
                {
                    AsFieldName = selectKey.GetKeyName(),
                    ContainsCalculatedField = containsCalc
                };
                return new Tuple<FieldDescription, string>(resultField, script);
            }
            // Select key not found, but '*' presented
            else if (parsedScript.Select.Keys.Any(o => o.IsStar))
            {
                var resultField = new FieldDescription(orderByKeyName, dbFieldDesc.Item2)
                {
                    AsFieldName = orderByKeyName,
                    ContainsCalculatedField = containsCalc
                };
                return new Tuple<FieldDescription, string>(resultField, script);
            }
            // Select key not found: we should update query
            else
            {
                var lastSelectKeyToken = parsedScript.Select.Keys.Last().Content.Tokens.Last();
                int lastSelectKeyPos = lastSelectKeyToken.Content.StartIndex + lastSelectKeyToken.Content.Length;
                string modifiedScript = script.Insert(lastSelectKeyPos, $", {orderByKey.KeyExpression.ToString()}");

                var resultField = new FieldDescription(orderByKeyName, dbFieldDesc.Item2)
                {
                    AsFieldName = orderByKeyName,
                    ContainsCalculatedField = containsCalc
                };
                return new Tuple<FieldDescription, string>(resultField, modifiedScript);
            }
        }

        public override Tuple<FieldDescription, string> PrepareKeyScript(string script, IUserCommandsHandler handler)
        {
            var parsedScript = PostgreSelectScript.Parse(script);
            if (parsedScript.OrderBy == null)
                return null;

            string keyName = handler.GetKeyName();
            FieldDescription keyDescription = new FieldDescription(keyName, handler.GetDbFieldsDescription().First(x => string.Compare(x.Item1, keyName, true) == 0).Item2);
            
            // Star presented
            if (parsedScript.Select.Keys.Any(o => o.IsStar))
                return new Tuple<FieldDescription, string>(keyDescription, script);

            // Search for select key
            SelectKeyElement selectKey = parsedScript.Select.Keys.FirstOrDefault(o => o.IsTableColumn && string.Compare(o.GetTableColumnName(), keyName, true) == 0);
            if (selectKey != null)
                return new Tuple<FieldDescription, string>(keyDescription, script);

            // Key not found: should update query
            var lastSelectKeyToken = parsedScript.Select.Keys.Last().Content.Tokens.Last();
            int lastSelectKeyPos = lastSelectKeyToken.Content.StartIndex + lastSelectKeyToken.Content.Length;
            string modifiedScript = script.Insert(lastSelectKeyPos, $", {keyName}");
            return new Tuple<FieldDescription, string>(keyDescription, modifiedScript);
        }

        public override List<FieldDescription> GetOrderKeys(string script, IUserCommandsHandler handler)
        {
            var result = new List<FieldDescription>();

            var parsedScript = PostgreSelectScript.Parse(script);
            var allFields = handler.GetDbFieldsDescription();

            foreach (var element in parsedScript.OrderBy.Keys)
            {
                if (element.IsCalculatable)
                {
                    if (element.GetKeyName() == "count")
                    {
                        result.Add(new FieldDescription(element.GetKeyName(), typeof (Int64)));
                    }
                }
                else
                {
                    result.Add(new FieldDescription(element.GetKeyName(),
                        allFields.First(x => string.Compare(x.Item1, element.GetKeyName(), true) == 0).Item2));
                }
            }

            return result;
        }

        #endregion


        #region New Select


        public string CreateOrderScript(string script, FieldDescription idDescription)
        {
            var parsedScript = PostgreSelectScript.Parse(script);
            if (parsedScript.OrderBy == null)
                throw new ArgumentException("Original script should be ordered");

            OrderType orderType = parsedScript.OrderBy.Keys[0].OrderType;
            if (orderType == OrderType.Asc)
                return OrderAsc(parsedScript, idDescription);
            if (orderType == OrderType.Desc)
                return OrderDesc(parsedScript, idDescription);

            return script;
        }


        #endregion

        #region Order
        
        private string OrderAsc(PostgreSelectScript script, FieldDescription idDescription)
        {
            StringBuilder result = new StringBuilder(script.Script.Length);
            var mainScript = script.RemovePreAndPostSelectPart();

            if (script.PreSelectPart.TokenCount > 0)
                result.Append(script.PreSelectPart.ToString()).AppendLine(" ;");

            if (script.With != null)
            {
                mainScript = mainScript.RemoveWith();
                result.AppendLine(script.With.ToString());
            }

            result.AppendLine($"SELECT * FROM ( {mainScript.Format()} ) AS UserSearchTable");

            if (idDescription.IsFirstAsk)
                result.AppendLine($"WHERE UserSearchTable.{idDescription.AsFieldName} >= @{idDescription.FieldName}");
            else
                result.AppendLine($"WHERE UserSearchTable.{idDescription.AsFieldName} > @{idDescription.FieldName}");

            // Conditionally apply ORDER BY if user script ordered differently
            if (script.OrderBy == null ||
                script.OrderBy.Keys[0].OrderType != OrderType.Asc ||
                string.Compare(script.OrderBy.Keys[0].GetKeyName(), idDescription.AsFieldName, true) != 0)
            {
                result.AppendLine($"ORDER BY UserSearchTable.{idDescription.AsFieldName} ASC");
            }

            result.AppendLine($"LIMIT {idDescription.PageSize}");

            return result.ToString();
        }

        private string OrderDesc(PostgreSelectScript script, FieldDescription idDescription)
        {
            StringBuilder result = new StringBuilder(script.Script.Length);
            var mainScript = script.RemovePreAndPostSelectPart();

            if (script.PreSelectPart.TokenCount > 0)
                result.Append(script.PreSelectPart.ToString()).AppendLine(" ;");

            if (script.With != null)
            {
                mainScript = mainScript.RemoveWith();
                result.AppendLine(script.With.ToString());
            }

            result.AppendLine($"SELECT * FROM ( {mainScript.Format()} ) AS UserSearchTable");

            if (!idDescription.IsFirstAsk)
                result.AppendLine($"WHERE UserSearchTable.{idDescription.AsFieldName} < @{idDescription.FieldName}");


            // Conditionally apply ORDER BY if user script ordered differently
            if (script.OrderBy == null ||
                script.OrderBy.Keys[0].OrderType != OrderType.Desc ||
                string.Compare(script.OrderBy.Keys[0].GetKeyName(), idDescription.AsFieldName, true) != 0)
            {
                result.AppendLine($"ORDER BY UserSearchTable.{idDescription.AsFieldName} DESC");
            }

            result.AppendLine($"LIMIT {idDescription.PageSize}");

            return result.ToString();
        }
        
        #endregion
    }
}
 
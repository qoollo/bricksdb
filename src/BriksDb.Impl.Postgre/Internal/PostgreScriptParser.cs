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
        #region Helpers

        private struct ScriptKeyDescription
        {
            public ScriptKeyDescription(string normalizedKeyName, OrderByKeyElement orderByKey, SelectKeyElement selectKey, Tuple<string, Type> dbFieldDesc)
            {
                NormalizedKeyName = normalizedKeyName;
                OrderByKey = orderByKey;
                SelectKey = selectKey;
                DbFieldDesc = dbFieldDesc;
            }

            public string NormalizedKeyName { get; }
            public OrderByKeyElement OrderByKey { get; }
            public SelectKeyElement SelectKey { get; }
            public Tuple<string, Type> DbFieldDesc { get; }
            public Type KeyType => DbFieldDesc?.Item2;
            public bool IsCalculatable => OrderByKey.IsCalculatable || (SelectKey != null && SelectKey.IsCalculatable);
        }


        private static ScriptKeyDescription GetKeyDescription(PostgreSelectScript parsedScript, List<Tuple<string, Type>> dbFieldDescs, OrderByKeyElement orderByKey)
        {
            string orderByKeyName = orderByKey.GetKeyName();

            // Search for select Key
            SelectKeyElement selectKey = null;
            if (orderByKey.IsTableQualified)
            {
                selectKey = parsedScript.Select.Keys.FirstOrDefault(o => o.IsTableColumn && PostgreHelper.AreNamesEqual(o.GetTableColumnName(), true, orderByKeyName, true));
            }
            else
            {
                selectKey = parsedScript.Select.Keys.FirstOrDefault(o => PostgreHelper.AreNamesEqual(o.GetKeyName(), true, orderByKeyName, true));
                if (selectKey == null)
                    selectKey = parsedScript.Select.Keys.FirstOrDefault(o => o.IsTableColumn && PostgreHelper.AreNamesEqual(o.GetTableColumnName(), true, orderByKeyName, true));
            }

            // Search for Field Description
            Tuple<string, Type> dbFieldDesc = null;
            if (selectKey != null && selectKey.IsTableColumn)
            {
                var columnName = selectKey.GetTableColumnName();
                dbFieldDesc = dbFieldDescs.FirstOrDefault(x => PostgreHelper.AreNamesEqual(x.Item1, false, columnName, true));
            }
            else
            {
                dbFieldDesc = dbFieldDescs.FirstOrDefault(x => PostgreHelper.AreNamesEqual(x.Item1, false, orderByKeyName, true));
            }

            return new ScriptKeyDescription(orderByKeyName, orderByKey, selectKey, dbFieldDesc);
        }

        private static FieldDescription CreateFieldDesc(PostgreSelectScript parsedScript, ScriptKeyDescription keyDesc)
        {
            if (!keyDesc.IsCalculatable)
            {
                var resultField = new FieldDescription(keyDesc.NormalizedKeyName, keyDesc.KeyType);

                if (keyDesc.SelectKey != null)
                    resultField.AsFieldName = keyDesc.SelectKey.GetKeyName();
                else
                    resultField.AsFieldName = keyDesc.NormalizedKeyName;

                return resultField;
            }
            else
            {
                if (keyDesc.NormalizedKeyName.IndexOf("count", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return new FieldDescription(keyDesc.NormalizedKeyName, typeof(long));
                }
                else
                {
                    throw new Exception($"Calculatable ORDER BY keys can only be named with Count and should habe BIGINT Type. Key: {keyDesc.NormalizedKeyName}");
                }
            }
        }

        #endregion


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

        private static bool CheckCalculatableFileds(PostgreSelectScript parsedScript, List<Tuple<string, Type>> dbFieldDescs)
        {
            bool result = false;
            foreach (var orderByKey in parsedScript.OrderBy.Keys)
            {
                var curOrderByDesc = GetKeyDescription(parsedScript, dbFieldDescs, orderByKey);
                if (curOrderByDesc.IsCalculatable)
                {
                    result = true;
                    if (curOrderByDesc.SelectKey == null)
                        throw new Exception($"Calculatable key should always be a part of SELECT statement. Field: {curOrderByDesc.NormalizedKeyName}");
                }
            }
            return result;
        }

        public override Tuple<FieldDescription, string> PrepareOrderScript(string script, int pageSize, IUserCommandsHandler handler)
        {
            string outputScript = script;
            var parsedScript = PostgreSelectScript.Parse(script);
            if (parsedScript.OrderBy == null || parsedScript.OrderBy.Keys.Count != 1)
                return null;

            var allFields = handler.GetDbFieldsDescription();

            // Order By key
            OrderByKeyElement orderByKey = parsedScript.OrderBy.Keys[0];
            var keyDesc = GetKeyDescription(parsedScript, allFields, orderByKey);

            // Contains calculatable fields
            bool containsCalculatable = CheckCalculatableFileds(parsedScript, allFields);


            // Db field not found
            if (keyDesc.DbFieldDesc == null && !keyDesc.IsCalculatable)
                return null;

            // Key not found as a part of selection process: we should update query
            if (keyDesc.SelectKey == null && !parsedScript.Select.Keys.Any(o => o.IsStar))
            {
                var lastSelectKeyToken = parsedScript.Select.Keys.Last().Content.Tokens.Last();
                int lastSelectKeyPos = lastSelectKeyToken.Content.StartIndex + lastSelectKeyToken.Content.Length;
                outputScript = script.Insert(lastSelectKeyPos, $", {orderByKey.KeyExpression.ToString()}");
            }

            var resultField = CreateFieldDesc(parsedScript, keyDesc);
            resultField.ContainsCalculatedField = containsCalculatable;
            return new Tuple<FieldDescription, string>(resultField, outputScript);
        }

        public override Tuple<FieldDescription, string> PrepareKeyScript(string script, IUserCommandsHandler handler)
        {
            var parsedScript = PostgreSelectScript.Parse(script);
            if (parsedScript.OrderBy == null)
                return null;

            string keyName = PostgreHelper.NormalizeName(handler.GetKeyName());
            FieldDescription keyDescription = new FieldDescription(keyName, handler.GetDbFieldsDescription().First(x => PostgreHelper.AreNamesEqual(x.Item1, false, keyName, true)).Item2);
            
            // Star presented
            if (parsedScript.Select.Keys.Any(o => o.IsStar))
                return new Tuple<FieldDescription, string>(keyDescription, script);

            // Search for select key
            SelectKeyElement selectKey = parsedScript.Select.Keys.FirstOrDefault(o => o.IsTableColumn && PostgreHelper.AreNamesEqual(o.GetTableColumnName(), true, keyName, true));
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

            foreach (var orderByKey in parsedScript.OrderBy.Keys)
            {
                var keyDesc = GetKeyDescription(parsedScript, allFields, orderByKey);
                result.Add(CreateFieldDesc(parsedScript, keyDesc));
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
                return OrderAsc(parsedScript, idDescription, new List<FieldDescription> { idDescription });
            if (orderType == OrderType.Desc)
                return OrderDesc(parsedScript, idDescription, new List<FieldDescription> { idDescription });

            return script;
        }

        public string CreateOrderScript(string script, FieldDescription idDescription, List<FieldDescription> keys)
        {
            var parsedScript = PostgreSelectScript.Parse(script);
            if (parsedScript.OrderBy == null)
                throw new ArgumentException("Original script should be ordered");

            OrderType orderType = parsedScript.OrderBy.Keys[0].OrderType;
            if (orderType == OrderType.Asc)
                return OrderAsc(parsedScript, idDescription, keys);
            if (orderType == OrderType.Desc)
                return OrderDesc(parsedScript, idDescription, keys);

            return script;
        }

        #endregion

        #region Order

        private string OrderAsc(PostgreSelectScript script, FieldDescription idDescription, List<FieldDescription> keys)
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
            {
                result.Append("WHERE ");
                for (int i = 0; i < keys.Count; i++)
                {
                    if (i != 0)
                        result.Append(" AND ");

                    if (i == keys.Count - 1)
                        result.AppendLine($"( UserSearchTable.{keys[i].AsFieldName} > @{keys[i].FieldName} )"); // Only last order key should be larger
                    else
                        result.AppendLine($"( UserSearchTable.{keys[i].AsFieldName} >= @{keys[i].FieldName} )");
                }
            }

            // Conditionally apply ORDER BY if user script ordered differently
            if (script.OrderBy == null || script.OrderBy.Keys.Count != keys.Count ||
                !script.OrderBy.Keys.Zip(keys, (a, b) => PostgreHelper.AreNamesEqual(a.GetKeyName(), true, b.AsFieldName, true)).All(o => o))
            {
                string order = string.Join(", ", keys.Select(o => $"UserSearchTable.{o.AsFieldName} ASC"));
                result.Append("ORDER BY ").AppendLine(order);
            }

            result.AppendLine($"LIMIT {idDescription.PageSize}");

            return result.ToString();
        }

        private string OrderDesc(PostgreSelectScript script, FieldDescription idDescription, List<FieldDescription> keys)
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
            {
                result.Append("WHERE ");
                for (int i = 0; i < keys.Count; i++)
                {
                    if (i != 0)
                        result.Append(" AND ");

                    if (i == keys.Count - 1)
                        result.AppendLine($"( UserSearchTable.{keys[i].AsFieldName} < @{keys[i].FieldName} )"); // Only last order key should be less
                    else
                        result.AppendLine($"( UserSearchTable.{keys[i].AsFieldName} <= @{keys[i].FieldName} )");
                }
            }

            // Conditionally apply ORDER BY if user script ordered differently
            if (script.OrderBy == null || script.OrderBy.Keys.Count != keys.Count ||
                !script.OrderBy.Keys.Zip(keys, (a, b) => PostgreHelper.AreNamesEqual(a.GetKeyName(), true, b.AsFieldName, true)).All(o => o))
            {
                string order = string.Join(", ", keys.Select(o => $"UserSearchTable.{o.AsFieldName} DESC"));
                result.Append("ORDER BY ").AppendLine(order);
            }

            result.AppendLine($"LIMIT {idDescription.PageSize}");

            return result.ToString();
        }

        #endregion
    }
}
 
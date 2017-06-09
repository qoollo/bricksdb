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

        public override Tuple<FieldDescription, string> PrepareOrderScript(string script, int pageSize, IUserCommandsHandler handler)
        {
            string outputScript = script;
            var parsedScript = PostgreSelectScript.Parse(script);
            if (parsedScript.OrderBy == null)
                return null;

            var allFields = handler.GetDbFieldsDescription();
            string keyName = PostgreHelper.NormalizeName(handler.GetKeyName());

            string additionalSelectKeys = "";
            bool containsCalculatable = false;
            bool containsStar = parsedScript.Select.Keys.Any(o => o.IsStar);

            // Check key name
            SelectKeyElement selectKey = parsedScript.Select.Keys.FirstOrDefault(o => o.IsTableColumn && PostgreHelper.AreNamesEqual(o.GetTableColumnName(), true, keyName, true));
            if (!containsStar && selectKey == null)
                additionalSelectKeys += $", {keyName}";

            // Check all Order By keys
            foreach (var orderByKey in parsedScript.OrderBy.Keys)
            {
                var keyDesc = GetKeyDescription(parsedScript, allFields, orderByKey);

                // Db field not found
                if (keyDesc.DbFieldDesc == null && !keyDesc.IsCalculatable)
                    throw new Exception($"ORDER BY key is not a part of table declared fields and is not calculatable. Field: {keyDesc.NormalizedKeyName}");

                // Verify calculatable fields
                if (keyDesc.IsCalculatable)
                {
                    containsCalculatable = true;
                    if (keyDesc.SelectKey == null)
                        throw new Exception($"Calculatable ORDER BY key should always be a part of SELECT statement. Field: {keyDesc.NormalizedKeyName}");
                }

                // When ORDER BY key is not presented in select result we should add additional keys
                if (keyDesc.SelectKey == null && !containsStar)
                    additionalSelectKeys += $", {orderByKey.KeyExpression.ToString()}";
            }

            // Insert additional select keys
            if (!string.IsNullOrEmpty(additionalSelectKeys))
            {
                var lastSelectKeyToken = parsedScript.Select.Keys.Last().Content.Tokens.Last();
                int lastSelectKeyPos = lastSelectKeyToken.Content.StartIndex + lastSelectKeyToken.Content.Length;
                outputScript = script.Insert(lastSelectKeyPos, additionalSelectKeys);
            }


            FieldDescription keyDescription = new FieldDescription(keyName, handler.GetDbFieldsDescription().First(x => PostgreHelper.AreNamesEqual(x.Item1, false, keyName, true)).Item2);
            keyDescription.ContainsCalculatedField = containsCalculatable;
            return new Tuple<FieldDescription, string>(keyDescription, outputScript);
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
            return CreateOrderScript(script, idDescription, new List<FieldDescription> { idDescription });
        }


        public string CreateOrderScript(string script, FieldDescription idDescription, List<FieldDescription> keys)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));
            if (keys.Count == 0)
                throw new ArgumentException("ORDER BY keys list cannot be empty", nameof(keys));

            var parsedScript = PostgreSelectScript.Parse(script);
            if (parsedScript.OrderBy == null)
                throw new ArgumentException("Original script should be ordered");


            // Check if script reordering required
            OrderType? reorderDirection = null;
            if (parsedScript.OrderBy.Keys.Count != keys.Count ||
                !parsedScript.OrderBy.Keys.Zip(keys, (a, b) => PostgreHelper.AreNamesEqual(a.GetKeyName(), true, b.AsFieldName, true)).All(o => o))
            {
                reorderDirection = parsedScript.OrderBy.Keys[0].OrderType; // Reorder according to the first key
            }


            StringBuilder result = new StringBuilder(parsedScript.Script.Length);
            var mainScript = parsedScript.RemovePreAndPostSelectPart();

            // Append PreSelect part
            if (parsedScript.PreSelectPart.TokenCount > 0)
                result.Append(parsedScript.PreSelectPart.ToString()).AppendLine(" ;");

            // Append WITH
            if (parsedScript.With != null)
            {
                mainScript = mainScript.RemoveWith();
                result.AppendLine(parsedScript.With.ToString());
            }

            // Append SELECT
            result.AppendLine($"SELECT * FROM ( {mainScript.Format()} ) AS UserSearchTable");

            // Append WHERE
            if (!idDescription.IsFirstAsk)
            {
                result.Append("WHERE (");

                for (int lastK = keys.Count - 1; lastK >= 0; lastK--)
                {
                    if (lastK != keys.Count - 1)
                        result.AppendLine(")").Append(" OR (");

                    // Append EQUAL keys
                    for (int i = 0; i < lastK; i++)
                    {
                        if (i != 0)
                            result.Append(" AND ");

                        result.Append($"( UserSearchTable.{keys[i].AsFieldName} = @{keys[i].FieldName} )");
                    }

                    if (lastK != 0)
                        result.Append(" AND ");

                    OrderType keyOrderType = reorderDirection ?? parsedScript.OrderBy.Keys[lastK].OrderType;
                    if (keyOrderType == OrderType.Desc)
                        result.Append($"( UserSearchTable.{keys[lastK].AsFieldName} < @{keys[lastK].FieldName} )");
                    else
                        result.Append($"( UserSearchTable.{keys[lastK].AsFieldName} > @{keys[lastK].FieldName} )");
                }

                result.AppendLine(")");
            }

            // Conditionally append ORDER BY if user script ordered differently
            if (reorderDirection != null)
            {
                string direction = reorderDirection == OrderType.Asc ? "ASC" : "DESC";
                string order = string.Join(", ", keys.Select(o => $"UserSearchTable.{o.AsFieldName} {direction}"));
                result.Append("ORDER BY ").AppendLine(order);
            }

            // Append LIMIT
            result.AppendLine($"LIMIT {idDescription.PageSize}");

            return result.ToString();
        }

        #endregion
    }
}
 
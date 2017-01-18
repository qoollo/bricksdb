using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Qoollo.Impl.Collector.Parser;
using Qoollo.Impl.Common.Data.Support;
using Qoollo.Impl.Common.Support;
using System.Text;

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

        #region ============== Script parsing ============


        // =============== LEXER =============

        internal struct ScriptPart
        {
            public ScriptPart(string script, int startIndex, int length)
            {
                if (script == null)
                    throw new ArgumentNullException(nameof(script));
                if (startIndex < 0 || startIndex > script.Length)
                    throw new ArgumentOutOfRangeException(nameof(startIndex));
                if (length < 0 || startIndex + length > script.Length)
                    throw new ArgumentOutOfRangeException(nameof(length));

                Script = script;
                StartIndex = startIndex;
                Length = length;
            }

            public string Script { get; private set; }
            public int StartIndex { get; private set; }
            public int Length { get; private set; }

            public override string ToString()
            {
                return Script.Substring(StartIndex, Length);
            }
        }


        internal enum TokenType
        {
            Unspecific,
            OpenBrace,
            CloseBrace,
            Comma,
            Semicolon,
            SingleQuoteString,
            DoubleQuoteString,

            WITH,
            SELECT,
            FROM,
            WHERE,
            GROUP_BY,
            HAVING,
            WINDOW,
            ORDER_BY,
            LIMIT,
            OFFSET,
            FETCH,
            FOR,
            UNION,
            INTERSECT,
            EXCEPT,

            ASC,
            DESC,
            AS
        }


        internal class ScriptToken
        {
            public ScriptToken(TokenType type, ScriptPart content)
            {
                Type = type;
                Content = content;
            }

            public TokenType Type { get; private set; }
            public ScriptPart Content { get; private set; }

            public override string ToString() { return $"{Type}: {Content}"; }
        }


        internal class TokenizedScript
        {
            private static readonly Regex s_tokenRegex = new Regex(
                @"\(|\)|\;|\,|ORDER\s+BY|GROUP\s+BY|'(?:[^'\\]|\\.|'')*'|""(?:[^""\\]|\\.|"""")*""|\*|\w+(?:\.\w+)?|\S", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            private static readonly Dictionary<string, TokenType> s_fixedMap = new Dictionary<string, TokenType>(StringComparer.OrdinalIgnoreCase)
            {
                ["("] = TokenType.OpenBrace,
                [")"] = TokenType.CloseBrace,
                [","] = TokenType.Comma,
                [";"] = TokenType.Semicolon,
                ["WITH"] = TokenType.WITH,
                ["SELECT"] = TokenType.SELECT,
                ["FROM"] = TokenType.FROM,
                ["WHERE"] = TokenType.WHERE,
                ["HAVING"] = TokenType.HAVING,
                ["WINDOW"] = TokenType.WINDOW,
                ["LIMIT"] = TokenType.LIMIT,
                ["OFFSET"] = TokenType.OFFSET,
                ["FETCH"] = TokenType.FETCH,
                ["FOR"] = TokenType.FOR,
                ["UNION"] = TokenType.UNION,
                ["INTERSECT"] = TokenType.INTERSECT,
                ["EXCEPT"] = TokenType.EXCEPT,
                ["ASC"] = TokenType.ASC,
                ["DESC"] = TokenType.DESC,
                ["AS"] = TokenType.AS,
            };

            public static TokenizedScript Parse(string script)
            {
                TokenizedScript result = new TokenizedScript();
                result.Script = script;

                var matches = s_tokenRegex.Matches(script);
                for (int i = 0; i < matches.Count; i++)
                {
                    TokenType tokenType = TokenType.Unspecific;
                    string matchValue = matches[i].Value;

                    if (!s_fixedMap.TryGetValue(matchValue, out tokenType))
                    {
                        if (matchValue.Length > 1 && matchValue.StartsWith("'") && matchValue.EndsWith("'"))
                            tokenType = TokenType.SingleQuoteString;
                        else if (matchValue.Length > 1 && matchValue.StartsWith("\"") && matchValue.EndsWith("\""))
                            tokenType = TokenType.DoubleQuoteString;
                        else if (matchValue.StartsWith("ORDER ", StringComparison.OrdinalIgnoreCase) && matchValue.EndsWith("BY", StringComparison.OrdinalIgnoreCase))
                            tokenType = TokenType.ORDER_BY;
                        else if (matchValue.StartsWith("GROUP ", StringComparison.OrdinalIgnoreCase) && matchValue.EndsWith("BY", StringComparison.OrdinalIgnoreCase))
                            tokenType = TokenType.GROUP_BY;
                    }

                    var token = new ScriptToken(tokenType, new ScriptPart(script, matches[i].Index, matches[i].Length));
                    result._tokens.Add(token);
                }
                

                return result;
            }



            private readonly List<ScriptToken> _tokens;
            public TokenizedScript() { Script = ""; _tokens = new List<ScriptToken>(); }

            public string Script { get; private set; }
            public IReadOnlyList<ScriptToken> Tokens { get { return _tokens; } }
        }

        // ============= Parser ===================


        internal struct TokenizedScriptPart
        {
            public TokenizedScriptPart(TokenizedScript script, int startToken, int tokenCount)
            {
                if (script == null)
                    throw new ArgumentNullException(nameof(script));
                if (startToken < 0 || startToken > script.Tokens.Count)
                    throw new ArgumentOutOfRangeException(nameof(startToken));
                if (tokenCount < 0 || startToken + tokenCount > script.Tokens.Count)
                    throw new ArgumentOutOfRangeException(nameof(tokenCount));

                Script = script;
                StartToken = startToken;
                TokenCount = tokenCount;
            }

            public TokenizedScript Script { get; private set; }
            public int StartToken { get; private set; }
            public int TokenCount { get; private set; }
            public ScriptToken this[int index] { get { return Script.Tokens[StartToken + index]; } }
            public IEnumerable<ScriptToken> Tokens { get { return Script.Tokens.Skip(StartToken).Take(TokenCount); } }


            public override string ToString()
            {
                if (TokenCount == 0)
                    return "";

                var token1 = Script.Tokens[StartToken];
                var token2 = Script.Tokens[StartToken + TokenCount - 1];
                return Script.Script.Substring(token1.Content.StartIndex, token2.Content.StartIndex + token2.Content.Length - token1.Content.StartIndex);
            }
        }


        internal class ScriptElement
        {
            protected static void SkipInsideBrace(TokenizedScript script, ref int tokenIndex)
            {
                if (script.Tokens[tokenIndex].Type != TokenType.OpenBrace)
                    return;

                int braceBalance = 1;
                tokenIndex++;
                while (tokenIndex < script.Tokens.Count && braceBalance > 0)
                {
                    if (script.Tokens[tokenIndex].Type == TokenType.OpenBrace)
                        braceBalance++;
                    else if (script.Tokens[tokenIndex].Type == TokenType.CloseBrace)
                        braceBalance--;

                    tokenIndex++;
                }
            }

            protected static void SkipUntil(TokenizedScript script, ref int tokenIndex, ISet<TokenType> untilTypes)
            {
                while (tokenIndex < script.Tokens.Count)
                {
                    if (script.Tokens[tokenIndex].Type == TokenType.OpenBrace)
                    {
                        SkipInsideBrace(script, ref tokenIndex);
                        continue;
                    }

                    if (untilTypes.Contains(script.Tokens[tokenIndex].Type))
                        break;

                    tokenIndex++;
                }
            }

            protected ScriptElement() { Content = new TokenizedScriptPart(new TokenizedScript(), 0, 0); }

            public TokenizedScriptPart Content { get; protected set; }
            public override string ToString() { return Content.ToString(); }
        }


        internal class WithClause: ScriptElement
        {
            private static readonly HashSet<TokenType> s_untilType = new HashSet<TokenType>() { TokenType.SELECT, TokenType.Semicolon };

            public static WithClause Parse(TokenizedScript script, ref int tokenIndex)
            {
                if (script.Tokens[tokenIndex].Type != TokenType.WITH)
                    throw new ArgumentException();

                WithClause result = new WithClause();
                int startToken = tokenIndex;
                SkipUntil(script, ref tokenIndex, s_untilType);
                result.Content = new TokenizedScriptPart(script, startToken, tokenIndex - startToken);
                return result;
            }

            private WithClause() { }
        }

        internal class SelectClause : ScriptElement
        {
            private static readonly HashSet<TokenType> s_untilType = new HashSet<TokenType>()
            {
                TokenType.Semicolon,
                TokenType.FROM,
                TokenType.WHERE,
                TokenType.GROUP_BY,
                TokenType.HAVING,
                TokenType.WINDOW,
                TokenType.UNION,
                TokenType.INTERSECT,
                TokenType.EXCEPT,
                TokenType.ORDER_BY,
                TokenType.LIMIT,
                TokenType.OFFSET,
                TokenType.FETCH,
                TokenType.FOR,
            };

            public static SelectClause Parse(TokenizedScript script, ref int tokenIndex)
            {
                if (script.Tokens[tokenIndex].Type != TokenType.SELECT)
                    throw new ArgumentException();

                SelectClause result = new SelectClause();
                int startToken = tokenIndex;
                SkipUntil(script, ref tokenIndex, s_untilType);
                result.Content = new TokenizedScriptPart(script, startToken, tokenIndex - startToken);
                return result;
            }

            private SelectClause() { }
        }

        internal class FromClause : ScriptElement
        {
            private static readonly HashSet<TokenType> s_untilType = new HashSet<TokenType>()
            {
                TokenType.Semicolon,
                TokenType.WHERE,
                TokenType.GROUP_BY,
                TokenType.HAVING,
                TokenType.WINDOW,
                TokenType.UNION,
                TokenType.INTERSECT,
                TokenType.EXCEPT,
                TokenType.ORDER_BY,
                TokenType.LIMIT,
                TokenType.OFFSET,
                TokenType.FETCH,
                TokenType.FOR,
            };

            public static FromClause Parse(TokenizedScript script, ref int tokenIndex)
            {
                if (script.Tokens[tokenIndex].Type != TokenType.FROM)
                    throw new ArgumentException();

                FromClause result = new FromClause();
                int startToken = tokenIndex;
                SkipUntil(script, ref tokenIndex, s_untilType);
                result.Content = new TokenizedScriptPart(script, startToken, tokenIndex - startToken);
                return result;
            }

            private FromClause() { }
        }

        internal class WhereClause : ScriptElement
        {
            private static readonly HashSet<TokenType> s_untilType = new HashSet<TokenType>()
            {
                TokenType.Semicolon,
                TokenType.GROUP_BY,
                TokenType.HAVING,
                TokenType.WINDOW,
                TokenType.UNION,
                TokenType.INTERSECT,
                TokenType.EXCEPT,
                TokenType.ORDER_BY,
                TokenType.LIMIT,
                TokenType.OFFSET,
                TokenType.FETCH,
                TokenType.FOR,
            };

            public static WhereClause Parse(TokenizedScript script, ref int tokenIndex)
            {
                if (script.Tokens[tokenIndex].Type != TokenType.WHERE)
                    throw new ArgumentException();

                WhereClause result = new WhereClause();
                int startToken = tokenIndex;
                SkipUntil(script, ref tokenIndex, s_untilType);
                result.Content = new TokenizedScriptPart(script, startToken, tokenIndex - startToken);
                return result;
            }

            private WhereClause() { }
        }

        internal class GroupByClause : ScriptElement
        {
            private static readonly HashSet<TokenType> s_untilType = new HashSet<TokenType>()
            {
                TokenType.Semicolon,
                TokenType.HAVING,
                TokenType.WINDOW,
                TokenType.UNION,
                TokenType.INTERSECT,
                TokenType.EXCEPT,
                TokenType.ORDER_BY,
                TokenType.LIMIT,
                TokenType.OFFSET,
                TokenType.FETCH,
                TokenType.FOR,
            };

            public static GroupByClause Parse(TokenizedScript script, ref int tokenIndex)
            {
                if (script.Tokens[tokenIndex].Type != TokenType.GROUP_BY)
                    throw new ArgumentException();

                GroupByClause result = new GroupByClause();
                int startToken = tokenIndex;
                SkipUntil(script, ref tokenIndex, s_untilType);
                result.Content = new TokenizedScriptPart(script, startToken, tokenIndex - startToken);
                return result;
            }

            private GroupByClause() { }
        }

        internal class OrderByKey : ScriptElement
        {
            private static readonly HashSet<TokenType> s_untilType = new HashSet<TokenType>()
            { 
                TokenType.Comma,
                TokenType.Semicolon,
                TokenType.LIMIT,
                TokenType.OFFSET,
                TokenType.FETCH,
                TokenType.FOR,
            };

            public static OrderByKey Parse(TokenizedScript script, ref int tokenIndex)
            {
                if (script.Tokens[tokenIndex].Type != TokenType.Unspecific)
                    throw new ArgumentException();

                OrderByKey result = new OrderByKey();
                int startToken = tokenIndex;
                SkipUntil(script, ref tokenIndex, s_untilType);
                result.Content = new TokenizedScriptPart(script, startToken, tokenIndex - startToken);
                if (result.Content.Tokens.Any(o => o.Type == TokenType.ASC))
                    result.OrderType = OrderType.Asc;
                else if (result.Content.Tokens.Any(o => o.Type == TokenType.DESC))
                    result.OrderType = OrderType.Desc;

                return result;
            }

            private OrderByKey() { }

            public OrderType OrderType { get; private set; }
        }


        internal class OrderByClause : ScriptElement
        {
            private static readonly HashSet<TokenType> s_untilType = new HashSet<TokenType>()
            {
                TokenType.Semicolon,
                TokenType.LIMIT,
                TokenType.OFFSET,
                TokenType.FETCH,
                TokenType.FOR,
            };

            public static OrderByClause Parse(TokenizedScript script, ref int tokenIndex)
            {
                if (script.Tokens[tokenIndex].Type != TokenType.ORDER_BY)
                    throw new ArgumentException();

                OrderByClause result = new OrderByClause();
                int startToken = tokenIndex;
                SkipUntil(script, ref tokenIndex, s_untilType);
                result.Content = new TokenizedScriptPart(script, startToken, tokenIndex - startToken);

                int keyStart = startToken + 1;
                int keyIndex = keyStart;
                while (keyIndex < result.Content.StartToken + result.Content.TokenCount)
                {
                    var key = OrderByKey.Parse(script, ref keyIndex);
                    result._keys.Add(key);
                    if (keyIndex >= result.Content.StartToken + result.Content.TokenCount)
                        break;

                    if (script.Tokens[keyIndex].Type != TokenType.Comma)
                        throw new PostgreScriptParsingException("Comma expected when parsing ORDER BY keys");

                    keyIndex++;
                }

                if (result.Keys.Count != 1)
                    throw new PostgreScriptParsingException("Only single key inside ORDER BY is supported");

                return result;
            }


            private List<OrderByKey> _keys = new List<OrderByKey>();
            private OrderByClause() { }

            public IReadOnlyList<OrderByKey> Keys { get { return _keys; } }
        }

        internal class SelectScript: ScriptElement
        {
            private static readonly HashSet<TokenType> s_untilType = new HashSet<TokenType>()
            {
                TokenType.SELECT,
                TokenType.WITH
            };

            public static SelectScript Parse(TokenizedScript script)
            {
                SelectScript result = new SelectScript();
                result.TokenizedScript = script;
                result.Content = new TokenizedScriptPart(script, 0, script.Tokens.Count);

                int tokenIndex = 0;
                // Skip to SELECT
                ScriptElement.SkipUntil(script, ref tokenIndex, s_untilType);
                result.PreSelectPart = new TokenizedScriptPart(script, 0, tokenIndex);
                if (tokenIndex == script.Tokens.Count)
                    return result;

                if (script.Tokens[tokenIndex].Type == TokenType.WITH)
                    result.With = WithClause.Parse(script, ref tokenIndex);
                if (tokenIndex == script.Tokens.Count || script.Tokens[tokenIndex].Type != TokenType.SELECT)
                    throw new PostgreScriptParsingException("SELECT expected after WITH");

                result.Select = SelectClause.Parse(script, ref tokenIndex);
                if (tokenIndex == script.Tokens.Count)
                    return result;

                if (script.Tokens[tokenIndex].Type == TokenType.FROM)
                {
                    result.From = FromClause.Parse(script, ref tokenIndex);
                    if (tokenIndex == script.Tokens.Count)
                        return result;
                }

                if (script.Tokens[tokenIndex].Type == TokenType.WHERE)
                {
                    result.Where = WhereClause.Parse(script, ref tokenIndex);
                    if (tokenIndex == script.Tokens.Count)
                        return result;
                }

                if (script.Tokens[tokenIndex].Type == TokenType.GROUP_BY)
                {
                    result.GroupBy = GroupByClause.Parse(script, ref tokenIndex);
                    if (tokenIndex == script.Tokens.Count)
                        return result;
                }

                if (script.Tokens[tokenIndex].Type == TokenType.ORDER_BY)
                {
                    result.OrderBy = OrderByClause.Parse(script, ref tokenIndex);
                    if (tokenIndex == script.Tokens.Count)
                        return result;
                }

                if (tokenIndex == script.Tokens.Count)
                    return result;
                if (script.Tokens[tokenIndex].Type != TokenType.Semicolon)
                    throw new PostgreScriptParsingException($"Unexpected or unsupported token inside script: {script.Tokens[tokenIndex].Type}");

                result.PostSelectPart = new TokenizedScriptPart(script, tokenIndex, script.Tokens.Count - tokenIndex);
                if (result.PostSelectPart.TokenCount > 0)
                {
                    if (result.PostSelectPart.TokenCount != 1 || result.PostSelectPart[0].Type != TokenType.Semicolon)
                        throw new PostgreScriptParsingException("Script should not contains any text after main SELECT Clause");
                }

                return result;
            }
            public static SelectScript Parse(string script)
            {
                return Parse(TokenizedScript.Parse(script));
            }


            private SelectScript()
            {
                TokenizedScript = new TokenizedScript();
                PreSelectPart = new TokenizedScriptPart(new TokenizedScript(), 0, 0);
                PostSelectPart = new TokenizedScriptPart(new TokenizedScript(), 0, 0);
            }

            public TokenizedScript TokenizedScript { get; private set; }
            public string Script { get { return TokenizedScript.Script; } }
            public TokenizedScriptPart PreSelectPart { get; private set; }

            public WithClause With { get; private set; }
            public SelectClause Select { get; private set; }
            public FromClause From { get; private set; }
            public WhereClause Where { get; private set; }
            public GroupByClause GroupBy { get; private set; }
            public OrderByClause OrderBy { get; private set; }

            public TokenizedScriptPart PostSelectPart { get; private set; }


            public SelectScript RemovePreAndPostSelectPart()
            {
                return new SelectScript()
                {
                    TokenizedScript = this.TokenizedScript,
                    PreSelectPart = new TokenizedScriptPart(new TokenizedScript(), 0, 0),
                    With = this.With,
                    Select = this.Select,
                    From = this.From,
                    Where = this.Where,
                    GroupBy = this.GroupBy,
                    OrderBy = this.OrderBy,
                    PostSelectPart = new TokenizedScriptPart(new TokenizedScript(), 0, 0)
                };
            }

            public SelectScript RemoveWith()
            {
                return new SelectScript()
                {
                    TokenizedScript = this.TokenizedScript,
                    PreSelectPart = this.PreSelectPart,
                    With = null,
                    Select = this.Select,
                    From = this.From,
                    Where = this.Where,
                    GroupBy = this.GroupBy,
                    OrderBy = this.OrderBy,
                    PostSelectPart = this.PostSelectPart
                };
            }

            public SelectScript RemoveOrderBy()
            {
                return new SelectScript()
                {
                    TokenizedScript = this.TokenizedScript,
                    PreSelectPart = this.PreSelectPart,
                    With = this.With,
                    Select = this.Select,
                    From = this.From,
                    Where = this.Where,
                    GroupBy = this.GroupBy,
                    OrderBy = null,
                    PostSelectPart = this.PostSelectPart
                };
            }


            public string Format()
            {
                StringBuilder builder = new StringBuilder(100);
                builder.Append(PreSelectPart.ToString()).AppendLine(" ");
                if (With != null)
                    builder.Append(With.ToString()).AppendLine(" ");
                builder.Append(Select.ToString()).AppendLine(" ");
                if (From != null)
                    builder.Append(From.ToString()).AppendLine(" ");
                if (Where != null)
                    builder.Append(Where.ToString()).AppendLine(" ");
                if (GroupBy != null)
                    builder.Append(GroupBy.ToString()).AppendLine(" ");
                if (OrderBy != null)
                    builder.Append(OrderBy.ToString()).AppendLine(" ");
                builder.Append(PostSelectPart.ToString());

                return builder.ToString();
            }
        }


        #endregion


        #region Public

        public override ScriptType ParseQueryType(string script)
        {
            var parsedScript = SelectScript.Parse(script);
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


        public string CreateOrderScript(string script, FieldDescription idDescription)
        {
            var parsedScript = SelectScript.Parse(script);
            if (parsedScript.OrderBy == null)
                throw new ArgumentException("Original script should be ordered");

            OrderType orderType = parsedScript.OrderBy.Keys[0].OrderType;
            if (orderType == OrderType.Asc)
                return OrderAsc(parsedScript.RemoveOrderBy(), idDescription);
            if (orderType == OrderType.Desc)
                return OrderDesc(parsedScript.RemoveOrderBy(), idDescription);

            return script;
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
        
        private string OrderAsc(SelectScript script, FieldDescription idDescription)
        {
            string compareType = idDescription.IsFirstAsk ? ">=" : ">";

            if (script.With == null)
            {
                var mainScript = script.RemovePreAndPostSelectPart();

                return $@"{script.PreSelectPart.ToString()} ;
                              SELECT * FROM ( {mainScript.Format()} ) AS UserSearchTable
                              WHERE UserSearchTable.{idDescription.AsFieldName} {compareType} @{idDescription.FieldName}
                              ORDER BY UserSearchTable.{idDescription.AsFieldName}
                              LIMIT {idDescription.PageSize}";
            }
            else
            {
                var mainScript = script.RemovePreAndPostSelectPart().RemoveWith();

                return $@"{script.PreSelectPart.ToString()} ;
                              {script.With.ToString()}
                              SELECT * FROM ( {mainScript.Format()} ) AS UserSearchTable
                              WHERE UserSearchTable.{idDescription.AsFieldName} {compareType} @{idDescription.FieldName}
                              ORDER BY UserSearchTable.{idDescription.AsFieldName}
                              LIMIT {idDescription.PageSize}";
            }
        }

        private string OrderDesc(SelectScript script, FieldDescription idDescription)
        {
            if (script.With == null)
            {
                var mainScript = script.RemovePreAndPostSelectPart();

                if (idDescription.IsFirstAsk)
                {
                    return $@"{script.PreSelectPart.ToString()} ;
                              SELECT * FROM ( {mainScript.Format()} ) AS UserSearchTable
                              ORDER BY UserSearchTable.{idDescription.AsFieldName} DESC
                              LIMIT {idDescription.PageSize}";
                }
                else
                {
                    return $@"{script.PreSelectPart.ToString()} ;
                              SELECT * FROM ( {mainScript.Format()} ) AS UserSearchTable
                              WHERE UserSearchTable.{idDescription.AsFieldName} < @{idDescription.FieldName}
                              ORDER BY UserSearchTable.{idDescription.AsFieldName} DESC
                              LIMIT {idDescription.PageSize}";
                }
            }
            else
            {
                var mainScript = script.RemovePreAndPostSelectPart().RemoveWith();

                if (idDescription.IsFirstAsk)
                {
                    return $@"{script.PreSelectPart.ToString()} ;
                              {script.With.ToString()}
                              SELECT * FROM ( {mainScript.Format()} ) AS UserSearchTable
                              ORDER BY UserSearchTable.{idDescription.AsFieldName} DESC
                              LIMIT {idDescription.PageSize}";
                }
                else
                {
                    return $@"{script.PreSelectPart.ToString()} ;
                              {script.With.ToString()}
                              SELECT * FROM ( {mainScript.Format()} ) AS UserSearchTable
                              WHERE UserSearchTable.{idDescription.AsFieldName} < @{idDescription.FieldName}
                              ORDER BY UserSearchTable.{idDescription.AsFieldName} DESC
                              LIMIT {idDescription.PageSize}";
                }
            }
        }
        
        #endregion
    }
}
 
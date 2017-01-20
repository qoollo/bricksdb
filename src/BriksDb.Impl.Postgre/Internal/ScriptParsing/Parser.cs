using Qoollo.Impl.Common.Data.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Impl.Postgre.Internal.ScriptParsing
{
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

        protected static void SkipUntil(TokenizedScript script, ref int tokenIndex, ISet<TokenType> untilTypes, int maxIndex = int.MaxValue, bool skipInitial = false)
        {
            if (skipInitial && tokenIndex < script.Tokens.Count)
                tokenIndex++;

            while (tokenIndex < script.Tokens.Count && tokenIndex < maxIndex)
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

        protected static T ParseSimple<T>(T elem, TokenizedScript script, ref int tokenIndex, ISet<TokenType> untilTypes) where T: ScriptElement
        {
            int startToken = tokenIndex;
            SkipUntil(script, ref tokenIndex, untilTypes, skipInitial: true);
            elem.Content = new TokenizedScriptPart(script, startToken, tokenIndex - startToken);
            return elem;
        }

        protected static T ParseSimple<T>(T elem, TokenType tokenValidation, TokenizedScript script, ref int tokenIndex, ISet<TokenType> untilTypes) where T : ScriptElement
        {
            if (script.Tokens[tokenIndex].Type != tokenValidation)
                throw new ArgumentException();

            int startToken = tokenIndex;
            SkipUntil(script, ref tokenIndex, untilTypes, skipInitial: true);
            elem.Content = new TokenizedScriptPart(script, startToken, tokenIndex - startToken);
            return elem;
        }


        protected ScriptElement()
        {
            Content = new TokenizedScriptPart(new TokenizedScript(), 0, 0);
        }

        public TokenizedScriptPart Content { get; protected set; }
        public override string ToString() { return Content.ToString(); }
    }


    // ============== Clauses ===============

    internal static class SelectCommonStopTokens
    {
        public static readonly HashSet<TokenType> s_untilType = new HashSet<TokenType>()
            {
                TokenType.Semicolon,
                TokenType.WITH,
                TokenType.SELECT,
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
    }


    internal class WithClause : ScriptElement
    {
        public static WithClause Parse(TokenizedScript script, ref int tokenIndex)
        {
            return ParseSimple(new WithClause(), TokenType.WITH, script, ref tokenIndex, SelectCommonStopTokens.s_untilType);
        }

        private WithClause() { }
    }

    internal class SelectClause : ScriptElement
    {
        public static SelectClause Parse(TokenizedScript script, ref int tokenIndex)
        {
            return ParseSimple(new SelectClause(), TokenType.SELECT, script, ref tokenIndex, SelectCommonStopTokens.s_untilType);
        }

        private SelectClause() { }
    }

    internal class FromClause : ScriptElement
    {
        public static FromClause Parse(TokenizedScript script, ref int tokenIndex)
        {
            return ParseSimple(new FromClause(), TokenType.FROM, script, ref tokenIndex, SelectCommonStopTokens.s_untilType);
        }

        private FromClause() { }
    }

    internal class WhereClause : ScriptElement
    {
        public static WhereClause Parse(TokenizedScript script, ref int tokenIndex)
        {
            return ParseSimple(new WhereClause(), TokenType.WHERE, script, ref tokenIndex, SelectCommonStopTokens.s_untilType);
        }

        private WhereClause() { }
    }

    internal class GroupByClause : ScriptElement
    {
        public static GroupByClause Parse(TokenizedScript script, ref int tokenIndex)
        {
            return ParseSimple(new GroupByClause(), TokenType.GROUP_BY, script, ref tokenIndex, SelectCommonStopTokens.s_untilType);
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

        private static readonly HashSet<TokenType> s_untilKeyExpr = new HashSet<TokenType>()
        {
            TokenType.ASC,
            TokenType.DESC,
            TokenType.USING,
            TokenType.NULLS,
            TokenType.Comma,
        };


        public static OrderByKey Parse(TokenizedScript script, ref int tokenIndex)
        {
            if (tokenIndex == 0)
                throw new ArgumentException("ORDER BY keys should follow after ORDER BY");
            if (script[tokenIndex - 1].Type != TokenType.ORDER_BY && script[tokenIndex - 1].Type != TokenType.Comma)
                throw new PostgreScriptParsingException($"ORDER BY keys should follow after ORDER BY: '{script.GetContextString(tokenIndex)}'");

            OrderByKey result = new OrderByKey();
            int startToken = tokenIndex;
            SkipUntil(script, ref tokenIndex, s_untilType);
            result.Content = new TokenizedScriptPart(script, startToken, tokenIndex - startToken);

            if (result.Content.Tokens.Any(o => o.Type == TokenType.ASC))
                result.OrderType = OrderType.Asc;
            else if (result.Content.Tokens.Any(o => o.Type == TokenType.DESC))
                result.OrderType = OrderType.Desc;

            int keyNameStart = startToken;
            int keyNameEnd = keyNameStart;
            SkipUntil(script, ref keyNameEnd, s_untilKeyExpr, maxIndex: tokenIndex);
            result.KeyNameExpression = new TokenizedScriptPart(script, keyNameStart, keyNameEnd - keyNameStart);

            return result;
        }

        private OrderByKey() { }

        public TokenizedScriptPart KeyNameExpression { get; private set; }
        public OrderType OrderType { get; private set; }

        public string GetNormalizedKeyName()
        {
            string result = KeyNameExpression.ToString();
            if (result.Length > 2 && result[0] == '(' && result[result.Length - 1] == ')')
                result = result.Substring(1, result.Length - 2);
            return result;
        }
    }


    internal class OrderByClause : ScriptElement
    {
        public static OrderByClause Parse(TokenizedScript script, ref int tokenIndex)
        {
            if (script.Tokens[tokenIndex].Type != TokenType.ORDER_BY)
                throw new ArgumentException();

            OrderByClause result = new OrderByClause();
            int startToken = tokenIndex;
            SkipUntil(script, ref tokenIndex, SelectCommonStopTokens.s_untilType, skipInitial: true);
            result.Content = new TokenizedScriptPart(script, startToken, tokenIndex - startToken);


            // Parse keys
            int keyStart = startToken + 1;
            int keyIndex = keyStart;
            while (keyIndex < result.Content.StartToken + result.Content.TokenCount)
            {
                var key = OrderByKey.Parse(script, ref keyIndex);
                result._keys.Add(key);
                if (keyIndex >= result.Content.StartToken + result.Content.TokenCount)
                    break;

                if (script.Tokens[keyIndex].Type != TokenType.Comma)
                    throw new PostgreScriptParsingException($"Comma expected when parsing ORDER BY keys: '{script.GetContextString(keyIndex)}'");

                keyIndex++;
            }

            if (result.Keys.Count != 1)
                throw new PostgreScriptParsingException($"Only single key inside ORDER BY is supported: '{result.Content.ToString()}'");

            return result;
        }


        private List<OrderByKey> _keys = new List<OrderByKey>();
        private OrderByClause() { }

        public IReadOnlyList<OrderByKey> Keys { get { return _keys; } }
    }


    internal class LimitClause : ScriptElement
    {
        public static LimitClause Parse(TokenizedScript script, ref int tokenIndex)
        {
            return ParseSimple(new LimitClause(), TokenType.LIMIT, script, ref tokenIndex, SelectCommonStopTokens.s_untilType);
        }

        private LimitClause() { }
    }

    internal class OffsetClause : ScriptElement
    {
        public static OffsetClause Parse(TokenizedScript script, ref int tokenIndex)
        {
            return ParseSimple(new OffsetClause(), TokenType.OFFSET, script, ref tokenIndex, SelectCommonStopTokens.s_untilType);
        }

        private OffsetClause() { }
    }



    internal class PostgreSelectScript : ScriptElement
    {
        private static readonly HashSet<TokenType> s_untilType = new HashSet<TokenType>()
            {
                TokenType.SELECT,
                TokenType.WITH
            };

        public static PostgreSelectScript Parse(TokenizedScript script)
        {
            PostgreSelectScript result = new PostgreSelectScript();
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
                throw new PostgreScriptParsingException($"SELECT expected after WITH: '{script.GetContextString(tokenIndex)}'");

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

            if (script.Tokens[tokenIndex].Type == TokenType.LIMIT)
            {
                result.Limit = LimitClause.Parse(script, ref tokenIndex);
                if (tokenIndex == script.Tokens.Count)
                    return result;
            }

            if (script.Tokens[tokenIndex].Type == TokenType.OFFSET)
            {
                result.Offset = OffsetClause.Parse(script, ref tokenIndex);
                if (tokenIndex == script.Tokens.Count)
                    return result;
            }

            if (tokenIndex == script.Tokens.Count)
                return result;
            if (script.Tokens[tokenIndex].Type != TokenType.Semicolon)
                throw new PostgreScriptParsingException($"Unexpected or unsupported token inside script. TokenType: {script.Tokens[tokenIndex].Type}, Context: '{script.GetContextString(tokenIndex)}'");

            result.PostSelectPart = new TokenizedScriptPart(script, tokenIndex, script.Tokens.Count - tokenIndex);
            if (result.PostSelectPart.TokenCount > 0)
            {
                if (result.PostSelectPart.TokenCount != 1 || result.PostSelectPart[0].Type != TokenType.Semicolon)
                    throw new PostgreScriptParsingException("Script should not contains any text after main SELECT: '{script.GetContextString(tokenIndex)}'");
            }

            return result;
        }
        public static PostgreSelectScript Parse(string script)
        {
            return Parse(TokenizedScript.Parse(script));
        }


        private PostgreSelectScript()
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
        public LimitClause Limit { get; private set; }
        public OffsetClause Offset { get; private set; }

        public TokenizedScriptPart PostSelectPart { get; private set; }


        public PostgreSelectScript RemovePreAndPostSelectPart()
        {
            return new PostgreSelectScript()
            {
                TokenizedScript = this.TokenizedScript,
                PreSelectPart = new TokenizedScriptPart(new TokenizedScript(), 0, 0),
                With = this.With,
                Select = this.Select,
                From = this.From,
                Where = this.Where,
                GroupBy = this.GroupBy,
                OrderBy = this.OrderBy,
                Limit = this.Limit,
                Offset = this.Offset,
                PostSelectPart = new TokenizedScriptPart(new TokenizedScript(), 0, 0)
            };
        }

        public PostgreSelectScript RemoveWith()
        {
            return new PostgreSelectScript()
            {
                TokenizedScript = this.TokenizedScript,
                PreSelectPart = this.PreSelectPart,
                With = null,
                Select = this.Select,
                From = this.From,
                Where = this.Where,
                GroupBy = this.GroupBy,
                OrderBy = this.OrderBy,
                Limit = this.Limit,
                Offset = this.Offset,
                PostSelectPart = this.PostSelectPart
            };
        }

        public PostgreSelectScript RemoveOrderBy()
        {
            return new PostgreSelectScript()
            {
                TokenizedScript = this.TokenizedScript,
                PreSelectPart = this.PreSelectPart,
                With = this.With,
                Select = this.Select,
                From = this.From,
                Where = this.Where,
                GroupBy = this.GroupBy,
                OrderBy = null,
                Limit = this.Limit,
                Offset = this.Offset,
                PostSelectPart = this.PostSelectPart
            };
        }


        public string Format()
        {
            StringBuilder builder = new StringBuilder(100);
            if (PreSelectPart.TokenCount > 0)
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
            if (Limit != null)
                builder.Append(Limit.ToString()).AppendLine(" ");
            if (Offset != null)
                builder.Append(Offset.ToString()).AppendLine(" ");
            if (PostSelectPart.TokenCount > 0)
                builder.Append(PostSelectPart.ToString());

            return builder.ToString();
        }
    }
}

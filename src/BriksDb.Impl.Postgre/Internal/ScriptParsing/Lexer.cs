using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Qoollo.Impl.Postgre.Internal.ScriptParsing
{
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
        USING,
        NULLS,

        AS,
        ALL,
        DISTINCT,
        ON
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
        private static readonly Regex s_spaceRemover = new Regex(@"\s+", RegexOptions.Compiled);
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
            ["GROUP BY"] = TokenType.GROUP_BY,
            ["HAVING"] = TokenType.HAVING,
            ["WINDOW"] = TokenType.WINDOW,
            ["ORDER BY"] = TokenType.ORDER_BY,
            ["LIMIT"] = TokenType.LIMIT,
            ["OFFSET"] = TokenType.OFFSET,
            ["FETCH"] = TokenType.FETCH,
            ["FOR"] = TokenType.FOR,
            ["UNION"] = TokenType.UNION,
            ["INTERSECT"] = TokenType.INTERSECT,
            ["EXCEPT"] = TokenType.EXCEPT,
            ["ASC"] = TokenType.ASC,
            ["DESC"] = TokenType.DESC,
            ["USING"] = TokenType.USING,
            ["NULLS"] = TokenType.NULLS,
            ["AS"] = TokenType.AS,
            ["ALL"] = TokenType.ALL,
            ["DISTINCT"] = TokenType.DISTINCT,
            ["ON"] = TokenType.ON,
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
                string matchValueNormalized = s_spaceRemover.Replace(matchValue, " ");

                if (!s_fixedMap.TryGetValue(matchValueNormalized, out tokenType))
                {
                    if (matchValue.Length > 1 && matchValue.StartsWith("'") && matchValue.EndsWith("'"))
                        tokenType = TokenType.SingleQuoteString;
                    else if (matchValue.Length > 1 && matchValue.StartsWith("\"") && matchValue.EndsWith("\""))
                        tokenType = TokenType.DoubleQuoteString;
                }

                var token = new ScriptToken(tokenType, new ScriptPart(script, matches[i].Index, matches[i].Length));
                result._tokens.Add(token);
            }


            return result;
        }


        // ==================

        private readonly List<ScriptToken> _tokens;
        public TokenizedScript()
        {
            Script = "";
            _tokens = new List<ScriptToken>();
        }

        public string Script { get; private set; }
        public IReadOnlyList<ScriptToken> Tokens { get { return _tokens; } }

        public ScriptToken this[int index] { get { return _tokens[index]; } }
        public int TokenCount { get { return _tokens.Count; } }


        public string GetContextString(int tokenIndex, int backLookup = 3, int frontLookup = 6)
        {
            StringBuilder builder = new StringBuilder();

            int start = Math.Max(0, tokenIndex - backLookup);
            int end = Math.Min(TokenCount, tokenIndex + frontLookup);

            for (int i = start; i < end; i++)
            {
                if (i == tokenIndex)
                    builder.Append(">>>>");

                builder.Append(Tokens[i].Content.ToString());
                if (i != end - 1)
                    builder.Append(" ");
            }

            return builder.ToString();
        }
    }
}

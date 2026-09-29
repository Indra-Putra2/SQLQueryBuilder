using SQLQueryBuilder.Core.SQLEnum.SQLExpression;
using SQLQueryBuilder.Core.SQLEnum.Tokenizer;
using SQLQueryBuilder.Core.SQLExpression;
using SQLQueryBuilder.Core.Tokenizer;
using SQLQueryBuilder.Model.Context;
using System.Collections.Frozen;

namespace SQLQueryBuilder.Helper
{
    internal static class SQLParserHelper
    {
        public static bool Expect(SQLToken current, SQLTokenType expectedToken) => current.Type == expectedToken;
        public static bool Expects(SQLToken current, FrozenSet<SQLTokenType> expectedTokens) => expectedTokens.Contains(current.Type);
        public static bool IsKeyword(SQLToken token, string keyword, SQLParserContext context)
        {
            return Expect(token, SQLTokenType.Keyword) && context.GetSlice(token).Equals(keyword, StringComparison.OrdinalIgnoreCase);
        }
        public static bool IsCurrentKeyword(SQLParserContext context, string keyword)
        {
            var token = context.Current;
            return Expect(token, SQLTokenType.Keyword) && context.GetSlice(token).Equals(keyword, StringComparison.OrdinalIgnoreCase);
        }
        public static string UnescapeStringLiteral(ReadOnlySpan<char> slice)
        {
            if (slice.Length >= 3 && (slice[0] == 'N' || slice[0] == 'n' || slice[0] == 'd' || slice[0] == 'D') && slice[1] == '\'' && slice[^1] == '\'')
            {
                slice = slice[2..^1];
            }

            else if (slice.Length >= 2 && slice[0] == '\'' && slice[^1] == '\'')
            {
                slice = slice[1..^1];
            }

            if (slice.IndexOf("''", StringComparison.Ordinal) < 0)
            {
                return slice.ToString();
            }

            return slice.ToString().Replace("''", "'");
        }
        public static string UnescapeIdentifier(ReadOnlySpan<char> slice)
        {
            if (slice.Length >= 2)
            {
                if (slice[0] == '"' && slice[^1] == '"')
                {
                    slice = slice[1..^1];

                    if (slice.IndexOf("\"\"", StringComparison.Ordinal) < 0)
                        return slice.ToString();

                    return slice.ToString().Replace("\"\"", "\"");
                }

                if (slice[0] == '[' && slice[^1] == ']')
                {
                    slice = slice[1..^1];

                    if (slice.IndexOf("]]", StringComparison.Ordinal) < 0)
                        return slice.ToString();

                    return slice.ToString().Replace("]]", "]");
                }
            }

            return slice.ToString();
        }
        public static string EscapeStringLiteral(string value)
        {
            if (value.IndexOf('\'') < 0)
            {
                return $"'{value.ToString()}'";
            }

            return $"'{value.ToString().Replace("'", "''")}'";
        }
        public static string EscapeIdentifierQuote(string value)
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        public static string EscapeIdentifierBracket(string value)
        {
            return $"[{value.Replace("]", "]]")}]";
        }
        public static SQLIdentifierDelimiter GetIdentifierDelimiter(SQLToken token)
        {
            return token.Type switch
            {
                SQLTokenType.BracketIdentifier => SQLIdentifierDelimiter.Bracket,
                SQLTokenType.DoubleQuoteIdentifier => SQLIdentifierDelimiter.DoubleQuote,
                _ => SQLIdentifierDelimiter.None,
            };
        }
        public static SQLSourceSpan CreateSpan(SQLToken token)
        {
            return new SQLSourceSpan { Start = token.StartPosition, Length = token.Length };
        }
        public static SQLSourceSpan CreateSpan(int startPosition, SQLSourceSpan endSpan)
        {
            int endPosition = endSpan.Start + endSpan.Length;
            return new SQLSourceSpan
            {
                Start = startPosition,
                Length = endPosition - startPosition
            };
        }
        public static SQLSourceSpan CreateSpan(int startPosition, int endStartPosition, int endLength)
        {
            int endPosition = endStartPosition + endLength;
            return new SQLSourceSpan
            {
                Start = startPosition,
                Length = endPosition - startPosition
            };
        }
    }
}

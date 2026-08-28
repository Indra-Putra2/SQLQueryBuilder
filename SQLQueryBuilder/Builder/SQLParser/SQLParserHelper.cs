using SQLQueryBuilder.Model;
using SQLQueryBuilder.SQLEnum;
using System.Collections.Frozen;

namespace SQLQueryBuilder.Builder.SQLParser
{
    internal class SQLParserHelper
    {
        //Variables
        private readonly SQLQueryContext _context;
        public int Index { get; private set; } = 0;
        public string Input => _context.OriginalText;
        public ReadOnlySpan<char> GetSlice(SQLToken token) => _context.GetSlice(token);

        //Methods
        public SQLParserHelper(SQLQueryContext context)
        {
            if (string.IsNullOrWhiteSpace(context.OriginalText)) throw new ArgumentNullException("Input Cannot be NULL or empty!");
            if (context.Tokens.Count == 0) throw new ArgumentNullException("Tokens cannot be NULL");
            if (context.Tokens.Count == 1 && context.Tokens[0].Type == SQLTokenType.EOF) throw new ArgumentNullException("Tokens cannot be Empty!");

            _context = context;
            Index = 0;
        }

        public SQLToken Current => Index < _context.Tokens.Count ? _context.Tokens[Index] : _context.Tokens[^1];
        public SQLToken Peek(int offset = 1)
        {
            int index = Index + offset;

            if (index < 0)
                return _context.Tokens[0];

            if (index >= _context.Tokens.Count)
                return _context.Tokens[^1];

            return _context.Tokens[index];
        }
        public SQLToken Consume() => Index < _context.Tokens.Count ? _context.Tokens[Index++] : _context.Tokens[^1];
        public bool IsAtEnd => Index >= _context.Tokens.Count;

        public bool Expect(SQLToken current, SQLTokenType expectedToken) => current.Type == expectedToken;
        public bool Expects(SQLToken current, FrozenSet<SQLTokenType> expectedTokens) => expectedTokens.Contains(current.Type);
        public bool IsKeyword(SQLToken token, string keyword)
        {
            return Expect(token, SQLTokenType.Keyword) && _context.GetSlice(token).Equals(keyword, StringComparison.OrdinalIgnoreCase);
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
        public static string EscapeIdentifier(string value)
        {
            if (value.IndexOf('"') < 0)
            {
                return $"\"{value.ToString()}\"";
            }

            return $"\"{value.ToString().Replace("\"", "\"\"")}\"";
        }
    }
}

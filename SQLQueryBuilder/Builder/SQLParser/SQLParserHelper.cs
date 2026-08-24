using SQLQueryBuilder.Model;
using SQLQueryBuilder.SQLEnum;
using System.Text;

namespace SQLQueryBuilder.Builder.SQLParser
{
    internal class SQLParserHelper
    {
        //Variables
        private readonly SQLQueryContext _context;
        public int Index { get; private set; } = 0;
        public string Input => _context.OriginalText;
        public ReadOnlySpan<char> GetSlice(SQLToken token) => _context.GetSlice(token);
        // Private Variables
        public static readonly Dictionary<SQLTokenType, SQLBinaryOperator> ComparisonOperators =
        new()
        {
            [SQLTokenType.Equal] = SQLBinaryOperator.Equal,
            [SQLTokenType.NotEqual] = SQLBinaryOperator.NotEqual,
            [SQLTokenType.LessThan] = SQLBinaryOperator.LessThan,
            [SQLTokenType.LessThanOrEqual] = SQLBinaryOperator.LessThanOrEqual,
            [SQLTokenType.GreaterThan] = SQLBinaryOperator.GreaterThan,
            [SQLTokenType.GreaterThanOrEqual] = SQLBinaryOperator.GreaterThanOrEqual,
        };
        // Definitions
        public static readonly HashSet<SQLTokenType> DelimitedIdentifiers =
        [
            SQLTokenType.BracketIdentifier,
            SQLTokenType.DoubleQuoteIdentifier,
        ];
        public static readonly HashSet<SQLTokenType> Identifiers =
        [
            SQLTokenType.Identifier,
            ..DelimitedIdentifiers
        ];

        public static readonly HashSet<SQLTokenType> Number =
        [
            SQLTokenType.IntegerLiteral,
            SQLTokenType.DecimalLiteral,
            SQLTokenType.ScientificLiteral,
        ];
        public static readonly HashSet<SQLTokenType> Literals =
        [
            ..Number,
            SQLTokenType.StringLiteral,
            SQLTokenType.UnicodeString,
            SQLTokenType.DatetimeLiteral,
            SQLTokenType.HexLiteral,
            SQLTokenType.MoneyLiteral,
            SQLTokenType.NullLiteral
        ];
        public static readonly HashSet<SQLTokenType> Comparison =
        [
            SQLTokenType.Equal,
            SQLTokenType.NotEqual,
            SQLTokenType.LessThan,
            SQLTokenType.LessThanOrEqual,
            SQLTokenType.GreaterThan,
            SQLTokenType.GreaterThanOrEqual,
        ];
        public static readonly HashSet<SQLTokenType> DelimitersAndTerminators =
        [
            SQLTokenType.Comma,
            SQLTokenType.Semicolon,
            SQLTokenType.EOF,
        ];
        public static readonly HashSet<SQLTokenType> AllowedAliases =
        [
            ..Identifiers,
            SQLTokenType.StringLiteral,
        ];

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
        public bool Expects(SQLToken current, HashSet<SQLTokenType> expectedTokens) => expectedTokens.Contains(current.Type);
        public bool IsKeyword(SQLToken token, string keyword)
        {
            return Expect(token, SQLTokenType.Keyword) && _context.GetSlice(token).Equals(keyword, StringComparison.OrdinalIgnoreCase);
        }
        public static bool TryGetSQLType(ReadOnlySpan<char> typeSpan, out SQLFunctionType type)
        {
            switch (typeSpan)
            {
                case var _ when typeSpan.Equals("NUMBER", StringComparison.OrdinalIgnoreCase):
                    type = SQLFunctionType.Number;
                    return true;
                case var _ when typeSpan.Equals("STRING", StringComparison.OrdinalIgnoreCase):
                    type = SQLFunctionType.String;
                    return true;
                case var _ when typeSpan.Equals("BOOL", StringComparison.OrdinalIgnoreCase):
                    type = SQLFunctionType.Boolean;
                    return true;
                case var _ when typeSpan.Equals("GUID", StringComparison.OrdinalIgnoreCase):
                    type = SQLFunctionType.Guid;
                    return true;
                case var _ when typeSpan.Equals("DATETIME", StringComparison.OrdinalIgnoreCase):
                    type = SQLFunctionType.DateTime;
                    return true;
                case var _ when typeSpan.Equals("BINARY", StringComparison.OrdinalIgnoreCase):
                    type = SQLFunctionType.Binary;
                    return true;
                default:
                    type = SQLFunctionType.Unknown;
                    return false;
            }
        }
    }
}

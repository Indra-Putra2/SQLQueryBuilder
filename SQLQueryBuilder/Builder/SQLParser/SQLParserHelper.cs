using SQLQueryBuilder.Model;
using SQLQueryBuilder.SQLEnum;
using System.Text;

namespace SQLQueryBuilder.Builder.SQLParser
{
    internal class SQLParserHelper
    {
        //Variables
        public string Input { get; private set; }
        public List<SQLToken> Tokens { get; private set; }
        public int Index { get; private set; } = 0;

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
        public static readonly Dictionary<string, SQLFunctionType> SQLTypes =
        new(StringComparer.InvariantCultureIgnoreCase)
        {
            ["NUMBER"] = SQLFunctionType.Number,
            ["STRING"] = SQLFunctionType.String,
            ["BOOL"] = SQLFunctionType.Boolean,
            ["GUID"] = SQLFunctionType.Guid,
            ["DATETIME"] = SQLFunctionType.DateTime,
            ["BINARY"] = SQLFunctionType.Binary,
        };
        // Definitions
        public static readonly HashSet<SQLTokenType> Identifiers =
        [
            SQLTokenType.Identifier,
            SQLTokenType.BracketIdentifier,
            SQLTokenType.DoubleQuoteIdentifier,
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
        public SQLParserHelper(string input, List<SQLToken> tokens)
        {
            if (IsStringNullOrEmpty(input)) throw new ArgumentNullException("Input Cannot be NULL or empty!");
            if (tokens.Count == 0) throw new ArgumentNullException("Tokens cannot be NULL or empty!");

            Input = input;
            Tokens = tokens;
            Index = 0;
        }

        public SQLToken Current => Index < Tokens.Count ? Tokens[Index] : Tokens[^1];
        public SQLToken Peek(int offset = 1)
        {
            int index = Index + offset;

            if (index < 0)
                return Tokens[0];

            if (index >= Tokens.Count)
                return Tokens[^1];

            return Tokens[index];
        }
        public SQLToken Consume() => Index < Tokens.Count ? Tokens[Index++] : Tokens[^1];
        public bool IsAtEnd => Index >= Tokens.Count;

        public bool Expect(SQLToken current, SQLTokenType expectedToken) => current.Type == expectedToken;
        public bool Expects(SQLToken current, HashSet<SQLTokenType> expectedTokens) => expectedTokens.Contains(current.Type);
        public bool IsStringNullOrEmpty(string input) => string.IsNullOrWhiteSpace(input);
        public bool IsKeyword(SQLToken token, string Keyword)
        {
            return Expect(token, SQLTokenType.Keyword) && string.Equals(token.Value, Keyword, StringComparison.InvariantCultureIgnoreCase);
        }
        public string CreateErrorMessage(string text, int errorIndexPos, string errorMessage, int errorTextLength = 1, int contextLength = 25)
        {
            errorTextLength = errorTextLength <= 0 ? 1 : errorTextLength;
            int endIdx = errorIndexPos + errorTextLength - 1;
            var context = GetContext(text, errorIndexPos, endIdx, contextLength);

            bool left = context.Start > 0;
            bool right = context.End < text.Length;

            var str = new StringBuilder();
            string trailingStr = "...";
            string leftDots = left ? trailingStr : "";
            string rightDots = right ? trailingStr : "";

            string enclosing = new string('=', context.Length + leftDots.Length + rightDots.Length);
            int pointerOffset = context.Pointer + leftDots.Length;
            string pointer = $"{new string(' ', pointerOffset)}{new string('^', errorTextLength)}";

            return str
                .Append('\n')
                .Append($"Error At Position: {errorIndexPos}\n")
                .Append(enclosing).Append('\n')
                .Append(leftDots).Append(context.Snippet).Append(rightDots).Append('\n')
                .Append(pointer).Append('\n')
                .Append(enclosing).Append('\n')
                .Append(errorMessage).ToString();
        }
        private (string Snippet, int Pointer, int Length, int Start, int End) GetContext(string text, int startIdx, int endIdx, int contextLength)
        {
            int start = Math.Max(0, startIdx - contextLength);
            int end = Math.Min(text.Length, endIdx + contextLength + 1);
            int length = end - start;
            return (text[start..end], startIdx - start, length, start, end);
        }
    }
}

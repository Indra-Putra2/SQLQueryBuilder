using SQLQueryBuilder.SQLEnum;
using SQLQueryBuilder.Model;
using System.Text;

namespace SQLQueryBuilder.Builder.SQLParser
{
    internal class SQLTokenizer
    {
        private List<SQLToken> _tokens = new();
        private string _input = string.Empty;
        private int _index = 0;

        private static readonly HashSet<string> _sqlKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            "SELECT", "FROM", "WHERE", "INSERT", "UPDATE", "DELETE",
            "INTO", "VALUES", "SET",
            "JOIN", "INNER", "LEFT", "RIGHT", "FULL", "OUTER", "ON",
            "AND", "OR", "NOT", "IN", "EXISTS", "BETWEEN", "LIKE", "IS",
            "ORDER", "BY", "GROUP", "HAVING", "DISTINCT",
            "AS",
            "CASE", "WHEN", "THEN", "ELSE", "END",
            "CREATE", "ALTER", "DROP", "TABLE", "VIEW", "INDEX",
            "LIMIT", "OFFSET", "DEFAULT",
            "ASC", "DESC",
            "TYPE",
        };

        private static readonly HashSet<string> _sqlDataTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "NUMBER", "STRING", "BOOL",
            "DATETIME",
            "BINARY",
            "GUID"
        };

        private char Current => _index < _input.Length ? _input[_index] : '\0';
        private char Peek(int offset = 1) => _index + offset < _input.Length ? _input[_index + offset] : '\0';

        private char Consume() => _index < _input.Length ? _input[_index++] : '\0';

        public List<SQLToken> Tokenize(string sqlQuery)
        {
            if (string.IsNullOrWhiteSpace(sqlQuery))
                throw new ArgumentNullException(nameof(sqlQuery), "Query string cannot be null or empty!");

            _tokens = new List<SQLToken>();
            _input = sqlQuery;
            _index = 0;

            while (Current != '\0')
            {
                var c = Current;

                // Skip Whitespace
                if (char.IsWhiteSpace(c))
                {
                    Consume();
                    continue;
                }

                // Comments (-- or /* */)
                if (c == '-' && Peek() == '-')
                {
                    SkipSingleLineComment();
                    continue;
                }
                if (c == '/' && Peek() == '*')
                {
                    SkipMultiLineComment();
                    continue;
                }

                // Numbers & Hex Literals
                if (char.IsDigit(c) || (c == '.' && char.IsDigit(Peek())))
                {
                    if (c == '0' && (Peek() == 'x' || Peek() == 'X'))
                    {
                        AddToken(SQLTokenType.HexLiteral, ParseHex);
                    }
                    else
                    {
                        var num = ParseNumber();

                        if (num.raw.Contains('e') || num.raw.Contains('E'))
                        {
                            AddToken(SQLTokenType.ScientificLiteral, () => num.raw.ToString());
                        }
                        else if (num.raw.Contains('.'))
                        {
                            AddToken(SQLTokenType.DecimalLiteral, () => num.raw.ToString());
                        }
                        else
                        {
                            AddToken(SQLTokenType.IntegerLiteral, () => num.raw.ToString());
                        }
                    }
                }
                // Unicode Strings (e.g. N'text')
                else if ((c == 'N' || c == 'n') && Peek() == '\'')
                {
                    AddToken(SQLTokenType.UnicodeString, ParseUnicode);
                }
                // Datetime Literals (e.g. d'2020-07-01')
                else if ((c == 'D' || c == 'd') && Peek() == '\'')
                {
                    AddToken(SQLTokenType.DatetimeLiteral, ParseDatetime);
                }
                // Variables (@var or @@var)
                else if (c == '@')
                {
                    AddToken(SQLTokenType.Variable, ParseVariable);
                }
                // Money Literals ($100)
                else if (c == '$' && char.IsDigit(Peek()))
                {
                    AddToken(SQLTokenType.MoneyLiteral, ParseMoney);
                }
                // Regular Identifiers OR Keywords OR NULL
                else if (char.IsLetter(c) || c == '_')
                {
                    var start = _index;
                    var iden = ParseIdentifierOrKeyword();
                    SQLTokenType type;

                    if (string.Equals(iden.clean, "NULL", StringComparison.OrdinalIgnoreCase))
                        type = SQLTokenType.NullLiteral;
                    else if (string.Equals(iden.clean, "FALSE", StringComparison.OrdinalIgnoreCase))
                        type = SQLTokenType.StringLiteral;
                    else if (_sqlKeywords.Contains(iden.clean))
                        type = SQLTokenType.Keyword;
                    else if (_sqlDataTypes.Contains(iden.clean))
                        type = SQLTokenType.SQLType;
                    else
                        type = SQLTokenType.Identifier;

                    _tokens.Add(new SQLToken(type, iden.raw, iden.clean, start, iden.raw.Length, iden.clean.Length));
                }
                // Quoted Identifiers
                else if (c == '[') AddToken(SQLTokenType.BracketIdentifier, () => ParseQuotedIdentifier(']'));
                else if (c == '"') AddToken(SQLTokenType.DoubleQuoteIdentifier, () => ParseQuotedIdentifier('"'));
                // Standard Strings
                else if (c == '\'') AddToken(SQLTokenType.StringLiteral, ParseString);
                // Single-Character Operators & Punctuation
                else if (c == '.') AddToken(SQLTokenType.Dot, () => Consume().ToString());
                else if (c == ',') AddToken(SQLTokenType.Comma, () => Consume().ToString());
                else if (c == ';') AddToken(SQLTokenType.Semicolon, () => Consume().ToString());
                else if (c == '(') AddToken(SQLTokenType.LeftParen, () => Consume().ToString());
                else if (c == ')') AddToken(SQLTokenType.RightParen, () => Consume().ToString());
                else if (c == '=') AddToken(SQLTokenType.Equal, () => Consume().ToString());

                // Multi-Character Operators & Punctuation & Bitwise
                else if (c == ':')
                {
                    if(Peek() == ':') AddToken(SQLTokenType.ScopeResolution, () => $"{Consume()}{Consume()}");
                    else AddToken(SQLTokenType.Unknown, () => Consume().ToString());
                }
                else if (c == '+')
                {
                    if (Peek() == '=') AddToken(SQLTokenType.PlusEquals, () => $"{Consume()}{Consume()}");
                    else AddToken(SQLTokenType.Plus, () => Consume().ToString());
                }
                else if (c == '-')
                {
                    if (Peek() == '=') AddToken(SQLTokenType.MinusEquals, () => $"{Consume()}{Consume()}");
                    else AddToken(SQLTokenType.Minus, () => Consume().ToString());
                }
                else if (c == '/')
                {
                    if (Peek() == '=') AddToken(SQLTokenType.DivideEquals, () => $"{Consume()}{Consume()}");
                    else AddToken(SQLTokenType.Divide, () => Consume().ToString());
                }
                else if (c == '*')
                {
                    if (Peek() == '=') AddToken(SQLTokenType.MultiplyEquals, () => $"{Consume()}{Consume()}");
                    else AddToken(SQLTokenType.Asterisk, () => Consume().ToString());
                }
                else if (c == '%')
                {
                    if (Peek() == '=') AddToken(SQLTokenType.ModuloEquals, () => $"{Consume()}{Consume()}");
                    else AddToken(SQLTokenType.Modulo, () => Consume().ToString());
                }
                
                else if (c == '<')
                {
                    if (Peek() == '=') AddToken(SQLTokenType.LessThanOrEqual, () => $"{Consume()}{Consume()}");
                    else if (Peek() == '>') AddToken(SQLTokenType.NotEqual, () => $"{Consume()}{Consume()}");
                    else if (Peek() == '<') AddToken(SQLTokenType.BitwiseLeftShift, () => $"{Consume()}{Consume()}");
                    else AddToken(SQLTokenType.LessThan, () => Consume().ToString());
                }
                else if (c == '>')
                {
                    if (Peek() == '=') AddToken(SQLTokenType.GreaterThanOrEqual, () => $"{Consume()}{Consume()}");
                    else if (Peek() == '>') AddToken(SQLTokenType.BitwiseRightShift, () => $"{Consume()}{Consume()}");
                    else AddToken(SQLTokenType.GreaterThan, () => Consume().ToString());
                }
                else if (c == '!')
                {
                    if (Peek() == '=') AddToken(SQLTokenType.NotEqual, () => $"{Consume()}{Consume()}");
                    else AddToken(SQLTokenType.Unknown, () => Consume().ToString());
                }

                // Bitwise Operator
                else if (c == '~') AddToken(SQLTokenType.BitwiseNot, () => Consume().ToString());
                else if (c == '&') AddToken(SQLTokenType.BitwiseAnd, () => Consume().ToString());
                else if (c == '|') AddToken(SQLTokenType.BitwiseOr, () => Consume().ToString());
                else if (c == '^') AddToken(SQLTokenType.BitwiseXor, () => Consume().ToString());

                else
                {
                    AddToken(SQLTokenType.Unknown, () => Consume().ToString());
                }
            }

            _tokens.Add(new SQLToken(SQLTokenType.EOF, "", "EOF", _input.Length, 0,0));
            return _tokens;
        }

        #region Helper Parsing Methods

        private void SkipSingleLineComment()
        {
            while (Current != '\0' && Current != '\n' && Current != '\r')
            {
                Consume();
            }
        }

        private void SkipMultiLineComment()
        {
            Consume(); // Consume '/'
            Consume(); // Consume '*'
            while (Current != '\0')
            {
                if (Current == '*' && Peek() == '/')
                {
                    Consume(); // Consume '*'
                    Consume(); // Consume '/'
                    break;
                }
                Consume();
            }
        }

        private (string raw, string clean) ParseVariable()
        {
            var clean = new StringBuilder();
            var raw = new StringBuilder();

            raw.Append(Consume()); // Consume @
            if (Current == '@')
            {
                raw.Append(Consume()); // Consume second @
            }
            var iden = ParseIdentifierOrKeyword();
            raw.Append(iden.raw);
            clean.Append(iden.clean);

            return (raw.ToString(), clean.ToString());
        }

        private (string raw, string clean) ParseMoney()
        {
            var clean = new StringBuilder();
            var raw = new StringBuilder();
            raw.Append(Consume()); // Consume $
            var num = ParseNumber();
            raw.Append(num.raw);
            clean.Append(num.clean);
            return (raw.ToString(), clean.ToString());
        }

        private (string raw, string clean) ParseUnicode()
        {
            var clean = new StringBuilder();
            var raw = new StringBuilder();
            raw.Append(Consume()); // Consume N/n
            var str = ParseString();
            raw.Append(str.raw);
            clean.Append(str.clean);
            return (raw.ToString(), clean.ToString());
        }
        private (string raw, string clean) ParseDatetime()
        {
            var clean = new StringBuilder();
            var raw = new StringBuilder();
            raw.Append(Consume()); // Consume D/d
            var str = ParseString();
            raw.Append(str.raw);
            clean.Append(str.clean);
            return (raw.ToString(), clean.ToString());
        }

        private (string raw, string clean) ParseHex()
        {
            var clean = new StringBuilder();
            var raw = new StringBuilder();
            raw.Append(Consume()); // Consume 0
            raw.Append(Consume()); // Consume x
            while (Current != '\0' && IsHexDigit(Current))
            {
                var current = Consume();
                raw.Append(current);
                clean.Append(current);
            }
            return (raw.ToString(), clean.ToString());
        }

        private (string raw, string clean) ParseString()
        {
            var clean = new StringBuilder();
            var raw = new StringBuilder();
            int startIndex = _index;
            raw.Append(Consume()); // Open single quote

            while (Current != '\0')
            {
                if (Current == '\'')
                {
                    if (Peek() == '\'')
                    {
                        var firstQuote = Consume();
                        raw.Append(firstQuote);
                        clean.Append(firstQuote);

                        raw.Append(Consume());
                        continue;
                    }

                    raw.Append(Consume()); // Close quote
                    return (raw.ToString(), clean.ToString());
                }
                var current = Consume();
                raw.Append(current);
                clean.Append(current);
            }

            throw new FormatException(CreateErrorMessage(_input, startIndex, "Unterminated string literal!"));
        }

        private (string raw, string clean) ParseQuotedIdentifier(char closingChar)
        {
            var clean = new StringBuilder();
            var raw = new StringBuilder();
            int startIndex = _index;
            raw.Append(Consume()); // Open bracket/quote

            while (Current != '\0')
            {
                if (Current == closingChar)
                {
                    if (Peek() == closingChar)
                    {
                        var firstQuote = Consume();
                        raw.Append(firstQuote);
                        clean.Append(firstQuote);

                        raw.Append(Consume());
                        continue;
                    }

                    raw.Append(Consume()); // Close bracket/quote
                    return (raw.ToString(), clean.ToString());
                }
                var current = Consume();
                raw.Append(current);
                clean.Append(current);
            }

            throw new FormatException(CreateErrorMessage(_input, startIndex, $"Unterminated quoted identifier, missing '{closingChar}'"));
        }

        private (string raw, string clean) ParseNumber()
        {
            var clean = new StringBuilder();
            var raw = new StringBuilder();
            var dotCount = 0;
            var hasDigit = false;

            // Mantissa
            while (char.IsDigit(Current) || Current == '.')
            {
                if (char.IsDigit(Current))
                    hasDigit = true;

                if (Current == '.')
                {
                    dotCount++;

                    if (dotCount > 1)
                    {
                        var err = CreateErrorMessage(_input, _index, $"Multiple decimal points '{clean}'");
                        throw new FormatException(err);
                    }
                }

                var c = Consume();
                raw.Append(c);
                clean.Append(c);
            }

            if (!hasDigit)
            {
                var err = CreateErrorMessage(_input, _index, $"Number requires digits '{clean}'");
                throw new FormatException(err);
            }

            // Scientific notation
            if (Current == 'e' || Current == 'E')
            {
                var c = Consume();
                raw.Append(c);
                clean.Append(c);


                if (Current == '+' || Current == '-')
                {
                    c = Consume();
                    raw.Append(c);
                    clean.Append(c);
                }


                if (!char.IsDigit(Current))
                {
                    var err = CreateErrorMessage(_input, _index, $"Invalid scientific notation '{clean}'");
                    throw new FormatException(err);
                }

                while (char.IsDigit(Current))
                {
                    c = Consume();
                    raw.Append(c);
                    clean.Append(c);
                }

                if (Current == 'e' || Current == 'E')
                {
                    var err = CreateErrorMessage(_input, _index, $"Too many Exponent Marker '{clean}'");
                    throw new FormatException(err);
                }

                if (Current == '.')
                {
                    var err = CreateErrorMessage(_input, _index, $"Exponent must be integer '{clean}'");
                    throw new FormatException(err);
                }
            }

            return (raw.ToString(), clean.ToString());
        }

        private (string raw, string clean) ParseIdentifierOrKeyword()
        {
            var clean = new StringBuilder();
            var raw = new StringBuilder();
            while (Current != '\0' && (char.IsLetterOrDigit(Current) || Current == '_'))
            {
                var current = Consume();
                raw.Append(current);
                clean.Append(current);
            }
            return (raw.ToString(), clean.ToString());
        }

        private static bool IsHexDigit(char c) =>
            (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');

        private void AddToken(SQLTokenType type, Func<(string raw, string clean)> operation)
        {
            var start = _index;
            var val = operation();

            var token = new SQLToken(type, val.raw, val.clean, start, val.raw.Length, val.clean.Length);
            _tokens.Add(token);
        }
        private void AddToken(SQLTokenType type, Func<string> operation)
        {
            var start = _index;
            var val = operation();

            var token = new SQLToken(type, val, val, start, val.Length, val.Length);
            _tokens.Add(token);
        }
        #endregion
        #region Error Formatting

        private (string Snippet, int Pointer, int Length, int Start, int End) GetContext(string text, int startIdx, int endIdx, int contextLength)
        {
            int start = Math.Max(0, startIdx - contextLength);
            int end = Math.Min(text.Length, endIdx + contextLength + 1);
            int length = end - start;
            return (text[start..end], startIdx - start, length, start, end);
        }

        private string CreateErrorMessage(string text, int errorIndexPos, string errorMessage, int errorTextLength = 1, int contextLength = 25)
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

        #endregion
    }
}
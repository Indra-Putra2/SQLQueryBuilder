using SQLQueryBuilder.Model;
using SQLQueryBuilder.SQLEnum;
using System.Collections.Frozen;

namespace SQLQueryBuilder.Builder.SQLParser
{
    internal class SQLTokenizer
    {
        private List<SQLToken> _tokens;
        private string _input;
        private int _index = 0;

        private static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> _sqlKeywords = new[]
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
            "ASC", "DESC"
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase).GetAlternateLookup<ReadOnlySpan<char>>();

        private static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> _sqlCustomKeywords = new[]
        {
            "TYPE"
         }.ToFrozenSet(StringComparer.OrdinalIgnoreCase).GetAlternateLookup<ReadOnlySpan<char>>();

        private static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> _sqlDataTypes = new[]
        {
            "NUMBER", "STRING", "BOOL", "DATETIME", "BINARY", "GUID"
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase).GetAlternateLookup<ReadOnlySpan<char>>();

        private char Current => _index < _input.Length ? _input[_index] : '\0';
        private char Peek(int offset = 1)
        {
            var index = _index + offset;

            if (index < 0)
                return _input[0];

            if (index < _input.Length)
                return _input[index];

            return '\0';
        }
        private char Consume() => _index < _input.Length ? _input[_index++] : '\0';

        public SQLQueryContext Tokenize(string sqlQuery)
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
                        ParseHex();
                    else
                        ParseNumber();
                }
                // Unicode Strings (e.g. N'text')
                else if ((c == 'N' || c == 'n') && Peek() == '\'')
                {
                    ParseString();
                }
                // Datetime Literals (e.g. d'2020-07-01')
                else if ((c == 'D' || c == 'd') && Peek() == '\'')
                {
                    ParseString();
                }
                // Variables (@var or @@var)
                else if (c == '@')
                {
                    ParseIdentifierOrKeyword();
                }
                // Money Literals ($100)
                else if (c == '$' && char.IsDigit(Peek()))
                {
                    ParseNumber();
                }
                // Regular Identifiers OR Keywords OR NULL
                else if (char.IsLetter(c) || c == '_')
                {
                    ParseIdentifierOrKeyword();
                }
                // Quoted Identifiers
                else if (c == '[') ParseDelimitedIdentifier(']');
                else if (c == '"') ParseDelimitedIdentifier('"');
                // Standard Strings
                else if (c == '\'') ParseString();
                // Single-Character Operators & Punctuation
                else if (c == '.') ConsumeAndAddToken(SQLTokenType.Dot, 1);
                else if (c == ',') ConsumeAndAddToken(SQLTokenType.Comma, 1);
                else if (c == ';') ConsumeAndAddToken(SQLTokenType.Semicolon, 1);
                else if (c == '(') ConsumeAndAddToken(SQLTokenType.LeftParen, 1);
                else if (c == ')') ConsumeAndAddToken(SQLTokenType.RightParen, 1);
                else if (c == '=') ConsumeAndAddToken(SQLTokenType.Equal, 1);

                // Multi-Character Operators & Punctuation & Bitwise
                else if (c == ':')
                {
                    if (Peek() == ':')
                        ConsumeAndAddToken(SQLTokenType.ScopeResolution, 2);
                    else
                        ConsumeAndAddToken(SQLTokenType.Unknown, 1);
                }
                else if (c == '+')
                {
                    if (Peek() == '=')
                        ConsumeAndAddToken(SQLTokenType.PlusEquals, 2);
                    else
                        ConsumeAndAddToken(SQLTokenType.Plus, 1);
                }
                else if (c == '-')
                {
                    if (Peek() == '=')
                        ConsumeAndAddToken(SQLTokenType.MinusEquals, 2);
                    else
                        ConsumeAndAddToken(SQLTokenType.Minus, 1);
                }
                else if (c == '/')
                {
                    if (Peek() == '=')
                        ConsumeAndAddToken(SQLTokenType.DivideEquals, 2);
                    else
                        ConsumeAndAddToken(SQLTokenType.Divide, 1);
                }
                else if (c == '*')
                {
                    if (Peek() == '=')
                        ConsumeAndAddToken(SQLTokenType.MultiplyEquals, 2);
                    else
                        ConsumeAndAddToken(SQLTokenType.Asterisk, 1);
                }
                else if (c == '%')
                {
                    if (Peek() == '=')
                        ConsumeAndAddToken(SQLTokenType.ModuloEquals, 2);
                    else
                        ConsumeAndAddToken(SQLTokenType.Modulo, 1);
                }

                else if (c == '<')
                {
                    if (Peek() == '=')
                        ConsumeAndAddToken(SQLTokenType.LessThanOrEqual, 2);
                    else if (Peek() == '>')
                        ConsumeAndAddToken(SQLTokenType.NotEqual, 2);
                    else if (Peek() == '<')
                        ConsumeAndAddToken(SQLTokenType.BitwiseLeftShift, 2);
                    else
                        ConsumeAndAddToken(SQLTokenType.LessThan, 1);
                }
                else if (c == '>')
                {
                    if (Peek() == '=')
                        ConsumeAndAddToken(SQLTokenType.GreaterThanOrEqual, 2);
                    else if (Peek() == '>')
                        ConsumeAndAddToken(SQLTokenType.BitwiseRightShift, 2);
                    else
                        ConsumeAndAddToken(SQLTokenType.GreaterThan, 1);
                }
                else if (c == '!')
                {
                    if (Peek() == '=') ConsumeAndAddToken(SQLTokenType.NotEqual, 2);
                    else ConsumeAndAddToken(SQLTokenType.Unknown, 1);
                }

                // Bitwise Operator
                else if (c == '~') ConsumeAndAddToken(SQLTokenType.BitwiseNot, 1);
                else if (c == '&') ConsumeAndAddToken(SQLTokenType.BitwiseAnd, 1);
                else if (c == '|') ConsumeAndAddToken(SQLTokenType.BitwiseOr, 1);
                else if (c == '^') ConsumeAndAddToken(SQLTokenType.BitwiseXor, 1);

                else
                {
                    ConsumeAndAddToken(SQLTokenType.Unknown, 1);
                }
            }

            _tokens.Add(new SQLToken(SQLTokenType.EOF, _input.Length, 0));

            return new SQLQueryContext(sqlQuery, _tokens);
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
            var start = _index;

            Consume(); // Consume '/'
            Consume(); // Consume '*'

            while (Current != '\0')
            {
                if (Current == '*' && Peek() == '/')
                {
                    Consume(); // Consume '*'
                    Consume(); // Consume '/'
                    return;
                }
                Consume();
            }

            throw new FormatException(
            ErrorFormatter.Create(
                _input,
                start,
                "Unterminated multi-line comment!"));
        }
        private void ParseHex()
        {
            var start = _index;

            Consume(); // Consume 0
            Consume(); // Consume x

            var digitStart = _index;

            while (Current != '\0' && IsHexDigit(Current))
                Consume();

            if (_index == digitStart)
            {
                var num = _input.Substring(start, _index - start);
                throw new FormatException(
                    ErrorFormatter.Create(
                        _input,
                        _index,
                        $"Hex literal requires digits '{num}'"));
            }

            AddToken(SQLTokenType.HexLiteral, start);
        }
        private void ParseString()
        {
            int start = _index;
            SQLTokenType type = SQLTokenType.StringLiteral;

            if (Current == 'd' || Current == 'D')
            {
                Consume(); // Consume d/D
                type = SQLTokenType.DatetimeLiteral;
            }
            else if (Current == 'n' || Current == 'N')
            {
                Consume(); // Consume d/D
                type = SQLTokenType.UnicodeString;
            }

            Consume(); // Consume opening quote

            while (Current != '\0')
            {
                if (Current == '\'')
                {
                    if (Peek() == '\'')
                    {
                        Consume(); // First quote
                        Consume(); // Second quote

                        continue;
                    }

                    Consume(); // Close quote
                    AddToken(type, start);
                    return;
                }
                Consume();
            }

            throw new FormatException(ErrorFormatter.Create(_input, _index, "Unterminated string literal!"));
        }
        private void ParseDelimitedIdentifier(char closingChar)
        {
            int start = _index;
            Consume(); // Opening quote/bracket

            while (Current != '\0')
            {
                if (Current == closingChar)
                {
                    if (Peek() == closingChar)
                    {
                        Consume(); // First quote/bracket
                        Consume(); // Second quote/bracket
                        continue;
                    }

                    Consume(); // Close quote/bracket
                    AddToken(
                        closingChar == ']'
                            ? SQLTokenType.BracketIdentifier
                            : SQLTokenType.DoubleQuoteIdentifier,
                        start);

                    return;
                }
                Consume();
            }

            throw new FormatException(ErrorFormatter.Create(_input, _index, $"Unterminated quoted identifier, missing '{closingChar}'"));
        }
        private void ParseNumber()
        {
            var start = _index;
            var dotCount = 0;
            var hasDigit = false;
            SQLTokenType type = SQLTokenType.IntegerLiteral;

            if (Current == '$')
            {
                type = SQLTokenType.MoneyLiteral;
                Consume();
            }

            while (char.IsDigit(Current) || Current == '.')
            {
                if (char.IsDigit(Current) && !hasDigit)
                    hasDigit = true;

                if (Current == '.')
                {
                    dotCount++;

                    if (dotCount > 1)
                    {
                        var num = _input.Substring(start, _index - start);
                        var err = ErrorFormatter.Create(_input, _index, $"Multiple decimal points '{num}'");
                        throw new FormatException(err);
                    }

                    if (type != SQLTokenType.MoneyLiteral)
                        type = SQLTokenType.DecimalLiteral;
                }

                Consume();
            }

            if (!hasDigit)
            {
                var num = _input.Substring(start, _index - start);
                var err = ErrorFormatter.Create(_input, _index, $"Number requires digits '{num}'");
                throw new FormatException(err);
            }

            // Scientific notation
            if (Current == 'e' || Current == 'E')
            {
                Consume();

                if (Current == '+' || Current == '-')
                    Consume();


                if (!char.IsDigit(Current))
                {
                    var num = _input.Substring(start, _index - start);
                    var err = ErrorFormatter.Create(_input, _index, $"Invalid scientific notation '{num}'");
                    throw new FormatException(err);
                }

                while (char.IsDigit(Current))
                    Consume();

                if (Current == 'e' || Current == 'E')
                {
                    var num = _input.Substring(start, _index - start);
                    var err = ErrorFormatter.Create(_input, _index, $"Too many Exponent Marker '{num}'");
                    throw new FormatException(err);
                }

                if (Current == '.')
                {
                    var num = _input.Substring(start, _index - start);
                    var err = ErrorFormatter.Create(_input, _index, $"Exponent must be integer '{num}'");
                    throw new FormatException(err);
                }

                type = SQLTokenType.ScientificLiteral;
            }

            AddToken(type, start);
        }
        private void ParseIdentifierOrKeyword()
        {
            var start = _index;
            SQLTokenType type = SQLTokenType.Identifier;

            if (Current == '@')
            {
                Consume();
                if (Current == '@')
                    Consume();
                type = SQLTokenType.Variable;
            }

            while (Current != '\0' && (char.IsLetterOrDigit(Current) || Current == '_'))
            {
                Consume();
            }

            var text = _input.AsSpan(start, _index - start);

            if (text.Equals("NULL", StringComparison.OrdinalIgnoreCase))
                type = SQLTokenType.NullLiteral;
            else if (_sqlKeywords.Contains(text))
                type = SQLTokenType.Keyword;
            else if (_sqlCustomKeywords.Contains(text))
                type = SQLTokenType.Keyword;
            else if (_sqlDataTypes.Contains(text))
                type = SQLTokenType.SQLType;

            AddToken(type, start);
        }
        private void ConsumeAndAddToken(SQLTokenType type, int amount)
        {
            var start = _index;

            if (amount < 0 || amount > _input.Length - _index)
                throw new ArgumentOutOfRangeException(nameof(amount));

            _index += amount;

            AddToken(type, start);
        }
        #endregion
        #region Helper
        private static bool IsHexDigit(char c) =>
            (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
        private void AddToken(SQLTokenType type, int start)
        {
            _tokens.Add(new SQLToken(
                type,
                start,
                _index - start));
        }
        #endregion
    }
}
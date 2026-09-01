using SQLQueryBuilder.Helper;
using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.Context;
using SQLQueryBuilder.SQLEnum;
using System.Globalization;

namespace SQLQueryBuilder.Builder.SQLParser
{
    internal class SQLTokenizer
    {
        private List<SQLToken> _tokens;
        private string _input;
        private int _index = 0;

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

        public SQLParserContext Tokenize(string sqlQuery)
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
                // Date Literals (e.g. d'2020-07-01')
                else if ((c == 'D' || c == 'd') && Peek() == '\'')
                {
                    ParseDate();
                }
                // Time Literals (e.g. t'14:30:00')
                else if ((c == 'T' || c == 't') && Peek() == '\'')
                {
                    ParseTime();
                }
                // Datetime Literals (e.g. dt'2020-07-01 14:30:00')
                else if ((c == 'D' || c == 'd') && (Peek() == 'T' || Peek() == 't') && Peek(2) == '\'')
                {
                    ParseDatetime();
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

                // Custom Syntax
                else if (c == '?') ConsumeAndAddToken(SQLTokenType.SQLTypeNullable, 1);
                else
                {
                    ConsumeAndAddToken(SQLTokenType.Unknown, 1);
                }
            }

            _tokens.Add(new SQLToken(SQLTokenType.EOF, _input.Length, 0));

            return new SQLParserContext(sqlQuery, _tokens);
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
        private void ParseDate()
        {
            int start = _index;
            var format = "YYYY-MM-DD";
            Consume(); // D/d
            Consume(); // '

            int year = ParseDatePart(start, "year", 4, format);
            ExpectHyphen(start, format);

            int month = ParseDatePart(start, "month", 2, format);
            ExpectHyphen(start, format);

            int day = ParseDatePart(start, "day", 2, format);

            if (Current != '\'')
                ThrowTemporalError(start, "Unterminated date literal.", format);

            ValidateDate(start, year, month, day, format);

            Consume(); // '

            AddToken(SQLTokenType.DateLiteral, start);
        }
        private void ParseTime()
        {
            var start = _index;
            var format = "HH:MM:SS[.fffffff]";

            Consume(); // T/t
            Consume(); // '

            var hours = ParseDatePart(start, "Hour", 2, format);
            ExpectColon(start, format);

            var minutes = ParseDatePart(start, "Minute", 2, format);
            ExpectColon(start, format);

            var seconds = ParseDateSeconds(start, format);

            if (Current != '\'')
                ThrowTemporalError(start, "Invalid time literal.", format);

            ValidateTime(start, hours, minutes, seconds, format);

            Consume(); // '

            AddToken(SQLTokenType.TimeLiteral, start);
        }
        private void ParseDatetime()
        {
            var start = _index;
            var format = "YYYY-MM-DD HH:MM:SS[.fffffff]";

            Consume(); // D/d
            Consume(); // T/t
            Consume(); // '

            int year = ParseDatePart(start, "year", 4, format);
            ExpectHyphen(start, format);

            int month = ParseDatePart(start, "month", 2, format);
            ExpectHyphen(start, format);

            int day = ParseDatePart(start, "day", 2, format);

            if (Current != ' ')
                ThrowTemporalError(start, "Invalid datetime literal.", format);

            Consume();

            var hours = ParseDatePart(start, "Hour", 2, format);
            ExpectColon(start, format);

            var minutes = ParseDatePart(start, "Minute", 2, format);
            ExpectColon(start, format);

            var seconds = ParseDateSeconds(start, format);

            if (Current != '\'')
                ThrowTemporalError(start, "Invalid datetime literal.", format);

            ValidateDate(start, year, month, day, format);
            ValidateTime(start, hours, minutes, seconds, format);

            Consume(); // '

            AddToken(SQLTokenType.DatetimeLiteral, start);
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

            if (Current == 'n' || Current == 'N')
            {
                Consume(); // Consume n/N
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
            else if (SQLDictionary.SQLKeywordsLookup.Contains(text))
                type = SQLTokenType.Keyword;
            else if (SQLDictionary.SQLCustomKeywordsLookup.Contains(text))
                type = SQLTokenType.Keyword;
            else if (SQLDictionary.SQLCustomDataTypeLookup.Contains(text))
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
        private int ParseDatePart(int start, string name, int expectedDigits, string format)
        {
            // 1. Ensure have enough characters left in the input to even read expectedDigits
            if (_index + expectedDigits > _input.Length)
                ThrowTemporalError(start, $"Invalid {name}.", format);

            // 2. Slice exact target window zero-allocation
            ReadOnlySpan<char> partSpan = _input.AsSpan(_index, expectedDigits);

            // 3. Fast-parse integer from span (implicitly validates digit format)
            if (!int.TryParse(partSpan, out int value))
            {
                ThrowTemporalError(start, $"Invalid {name}.", format);
            }

            // Advance position by expectedDigits
            _index += expectedDigits;

            return value;
        }
        private decimal ParseDateSeconds(int start, string format)
        {
            int partStart = _index;

            // 1. Advance the lexer pointer past all valid second characters (digits and dot)
            while (Current != '\0' && (char.IsDigit(Current) || Current == '.'))
            {
                Consume();
            }

            // 2. Extract zero-allocation slice of the seconds segment
            ReadOnlySpan<char> secondsSpan = _input.AsSpan(partStart, _index - partStart);

            if (secondsSpan.IsEmpty)
            {
                ThrowTemporalError(start, "Invalid seconds.", format);
            }

            // 3. Fast structural validation on the span
            int dotIndex = secondsSpan.IndexOf('.');
            if (dotIndex != -1)
            {
                // Check for multiple decimal points
                if (secondsSpan[(dotIndex + 1)..].Contains('.'))
                {
                    ThrowTemporalError(start, "Multiple decimal points.", format);
                }

                // Check fractional digit count (max 7 digits)
                int fractionalCount = secondsSpan.Length - (dotIndex + 1);
                if (fractionalCount > 7)
                {
                    ThrowTemporalError(start, $"Seconds cannot have more than 7 fractional digits.", format);
                }
            }

            // 4. Single-pass parse directly off the span
            if (!decimal.TryParse(secondsSpan, CultureInfo.InvariantCulture, out decimal value))
            {
                ThrowTemporalError(start, "Invalid seconds.", format);
            }

            return value;
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
        private void ThrowTemporalError(int start, string message, string format)
        {
            var num = _input.Substring(start, _index - start);
            var err = ErrorFormatter.Create(_input, _index, $"{message} {num}. Expected format: {format}.");
            throw new FormatException(err);
        }
        private void ExpectHyphen(int start, string format)
        {
            if (Current != '-')
                ThrowTemporalError(start, "Expected '-' in date.", format);

            Consume();
        }
        private void ExpectColon(int start, string format)
        {
            if (Current != ':')
                ThrowTemporalError(start, "Expected ':' in time.", format);

            Consume();
        }
        private void ValidateTime(int start, int hours, int minutes, decimal seconds, string format)
        {
            if (hours is < 0 or > 23)
                ThrowTemporalError(start, $"Invalid hours {hours}.", format);

            if (minutes is < 0 or > 59)
                ThrowTemporalError(start, $"Invalid minutes {minutes}.", format);

            if (seconds < 0 || seconds >= 60)
                ThrowTemporalError(start, $"Invalid seconds {seconds}.", format);
        }
        private void ValidateDate(int start, int year, int month, decimal day, string format)
        {
            if (month is < 1 or > 12)
                ThrowTemporalError(start, $"Invalid month {month}.", format);

            int maxDay = SQLDictionary._daysInMonth[month - 1];

            if (day is < 1 || day > maxDay)
                ThrowTemporalError(start, $"Invalid day {day}.", format);
        }
        #endregion
    }
}
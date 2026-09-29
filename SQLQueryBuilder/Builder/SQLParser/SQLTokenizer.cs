using SQLQueryBuilder.Core.SQLEnum.Tokenizer;
using SQLQueryBuilder.Core.Tokenizer;
using SQLQueryBuilder.Helper;
using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.Context;
using System.Globalization;

namespace SQLQueryBuilder.Builder.SQLParser
{
    internal static class SQLTokenizer
    {
        public static SQLParserContext Tokenize(string sqlQuery)
        {
            if (string.IsNullOrWhiteSpace(sqlQuery))
                throw new ArgumentNullException("Query string cannot be null or empty!");

            var context = new SQLTokenizerContext(sqlQuery.AsSpan());

            while (context.Current != '\0')
            {
                var c = context.Current;

                // Skip Whitespace
                if (char.IsWhiteSpace(c))
                {
                    context.Consume();
                    continue;
                }

                // Comments (-- or /* */)
                if (c == '-' && context.Peek() == '-')
                {
                    SkipSingleLineComment(ref context);
                    continue;
                }
                if (c == '/' && context.Peek() == '*')
                {
                    SkipMultiLineComment(ref context);
                    continue;
                }

                // Numbers & Hex Literals
                if (char.IsDigit(c) || (c == '.' && char.IsDigit(context.Peek())))
                {
                    if (c == '0' && (context.Peek() == 'x' || context.Peek() == 'X'))
                        ParseHex(ref context);
                    else
                        ParseNumber(ref context);
                }
                // Unicode Strings (e.g. N'text')
                else if ((c == 'N' || c == 'n') && context.Peek() == '\'')
                {
                    ParseString(ref context);
                }
                // Date Literals (e.g. d'2020-07-01')
                else if ((c == 'D' || c == 'd') && context.Peek() == '\'')
                {
                    ParseDate(ref context);
                }
                // Time Literals (e.g. t'14:30:00')
                else if ((c == 'T' || c == 't') && context.Peek() == '\'')
                {
                    ParseTime(ref context);
                }
                // Datetime Literals (e.g. dt'2020-07-01 14:30:00')
                else if ((c == 'D' || c == 'd') && (context.Peek() == 'T' || context.Peek() == 't') && context.Peek(2) == '\'')
                {
                    ParseDatetime(ref context);
                }
                // Variables (@var or @@var)
                else if (c == '@')
                {
                    ParseIdentifierOrKeyword(ref context);
                }
                // Money Literals ($100)
                else if (c == '$' && char.IsDigit(context.Peek()))
                {
                    ParseNumber(ref context);
                }
                // Regular Identifiers OR Keywords OR NULL
                else if (char.IsLetter(c) || c == '_')
                {
                    ParseIdentifierOrKeyword(ref context);
                }
                // Quoted Identifiers
                else if (c == '[') ParseDelimitedIdentifier(']', ref context);
                else if (c == '"') ParseDelimitedIdentifier('"', ref context);
                // Standard Strings
                else if (c == '\'') ParseString(ref context);
                // Single-Character Operators & Punctuation
                else if (c == '.') ConsumeAndAddToken(SQLTokenType.Dot, 1, ref context);
                else if (c == ',') ConsumeAndAddToken(SQLTokenType.Comma, 1, ref context);
                else if (c == ';') ConsumeAndAddToken(SQLTokenType.Semicolon, 1, ref context);
                else if (c == '(') ConsumeAndAddToken(SQLTokenType.LeftParen, 1, ref context);
                else if (c == ')') ConsumeAndAddToken(SQLTokenType.RightParen, 1, ref context);
                else if (c == '=') ConsumeAndAddToken(SQLTokenType.Equal, 1, ref context);

                // Multi-Character Operators & Punctuation & Bitwise
                else if (c == ':')
                {
                    if (context.Peek() == ':')
                        ConsumeAndAddToken(SQLTokenType.ScopeResolution, 2, ref context);
                    else
                        ConsumeAndAddToken(SQLTokenType.Unknown, 1, ref context);
                }
                else if (c == '+')
                {
                    if (context.Peek() == '=')
                        ConsumeAndAddToken(SQLTokenType.PlusEquals, 2, ref context);
                    else
                        ConsumeAndAddToken(SQLTokenType.Plus, 1, ref context);
                }
                else if (c == '-')
                {
                    if (context.Peek() == '=')
                        ConsumeAndAddToken(SQLTokenType.MinusEquals, 2, ref context);
                    else
                        ConsumeAndAddToken(SQLTokenType.Minus, 1, ref context);
                }
                else if (c == '/')
                {
                    if (context.Peek() == '=')
                        ConsumeAndAddToken(SQLTokenType.DivideEquals, 2, ref context);
                    else
                        ConsumeAndAddToken(SQLTokenType.Divide, 1, ref context);
                }
                else if (c == '*')
                {
                    if (context.Peek() == '=')
                        ConsumeAndAddToken(SQLTokenType.MultiplyEquals, 2, ref context);
                    else
                        ConsumeAndAddToken(SQLTokenType.Asterisk, 1, ref context);
                }
                else if (c == '%')
                {
                    if (context.Peek() == '=')
                        ConsumeAndAddToken(SQLTokenType.ModuloEquals, 2, ref context);
                    else
                        ConsumeAndAddToken(SQLTokenType.Modulo, 1, ref context);
                }

                else if (c == '<')
                {
                    if (context.Peek() == '=')
                        ConsumeAndAddToken(SQLTokenType.LessThanOrEqual, 2, ref context);
                    else if (context.Peek() == '>')
                        ConsumeAndAddToken(SQLTokenType.NotEqual, 2, ref context);
                    else if (context.Peek() == '<')
                        ConsumeAndAddToken(SQLTokenType.BitwiseLeftShift, 2, ref context);
                    else
                        ConsumeAndAddToken(SQLTokenType.LessThan, 1, ref context);
                }
                else if (c == '>')
                {
                    if (context.Peek() == '=')
                        ConsumeAndAddToken(SQLTokenType.GreaterThanOrEqual, 2, ref context);
                    else if (context.Peek() == '>')
                        ConsumeAndAddToken(SQLTokenType.BitwiseRightShift, 2, ref context);
                    else
                        ConsumeAndAddToken(SQLTokenType.GreaterThan, 1, ref context);
                }
                else if (c == '!')
                {
                    if (context.Peek() == '=') ConsumeAndAddToken(SQLTokenType.NotEqual, 2, ref context);
                    else ConsumeAndAddToken(SQLTokenType.Unknown, 1, ref context);
                }

                // Bitwise Operator
                else if (c == '~') ConsumeAndAddToken(SQLTokenType.BitwiseNot, 1, ref context);
                else if (c == '&') ConsumeAndAddToken(SQLTokenType.BitwiseAnd, 1, ref context);
                else if (c == '|') ConsumeAndAddToken(SQLTokenType.BitwiseOr, 1, ref context);
                else if (c == '^') ConsumeAndAddToken(SQLTokenType.BitwiseXor, 1, ref context);

                // Custom Syntax
                else if (c == '?') ConsumeAndAddToken(SQLTokenType.SQLTypeNullable, 1, ref context);
                else
                {
                    ConsumeAndAddToken(SQLTokenType.Unknown, 1, ref context);
                }
            }

            context.AddToken(new SQLToken(SQLTokenType.EOF, context.Span.Length, 0));

            return new SQLParserContext(context.Span, context.Tokens);
        }

        #region Helper Parsing Methods
        private static void SkipSingleLineComment(ref SQLTokenizerContext context)
        {
            while (context.Current != '\0' && context.Current != '\n' && context.Current != '\r')
            {
                context.Consume();
            }
        }
        private static void SkipMultiLineComment(ref SQLTokenizerContext context)
        {
            var start = context.Index;

            context.Consume(); // Consume '/'
            context.Consume(); // Consume '*'

            while (context.Current != '\0')
            {
                if (context.Current == '*' && context.Peek() == '/')
                {
                    context.Consume(); // Consume '*'
                    context.Consume(); // Consume '/'
                    return;
                }
                context.Consume();
            }

            throw new FormatException(
            ErrorFormatter.Create(
                context.Span.ToString(),
                start,
                "Unterminated multi-line comment!"));
        }
        private static void ParseDate(ref SQLTokenizerContext context)
        {
            var start = context.Index;

            context.Consume(); // D/d
            context.Consume(); // '

            // Initiate Year context
            var dateContext = new SQLTokenizerDateTimeContext(context.Index, 4, "Year", "YYYY-MM-DD");

            dateContext.Year = ParseDatePart(ref context, ref dateContext);
            ExpectHyphen(ref context, ref dateContext);

            // Change to Month Context
            dateContext.Start = context.Index;
            dateContext.ExpectedDigit = 2;
            dateContext.Name = "Month";

            dateContext.Month = ParseDatePart(ref context, ref dateContext);
            ExpectHyphen(ref context, ref dateContext);

            // Change to Day Context
            dateContext.Start = context.Index;
            dateContext.Name = "Day";

            dateContext.Day = ParseDatePart(ref context, ref dateContext);

            if (context.Current != '\'')
                ThrowTemporalError("Unterminated date literal.", ref context, ref dateContext);

            ValidateDate(ref context, ref dateContext);

            context.Consume(); // '

            AddToken(SQLTokenType.DateLiteral, start, ref context);
        }
        private static void ParseTime(ref SQLTokenizerContext context)
        {
            var start = context.Index;

            context.Consume(); // T/t
            context.Consume(); // '

            // Initiate Hour context
            var dateContext = new SQLTokenizerDateTimeContext(context.Index, 2, "Hour", "HH:MM:SS[.fffffff]");

            dateContext.Hour = ParseDatePart(ref context, ref dateContext);
            ExpectColon(ref context, ref dateContext);

            // Change to Minute Context
            dateContext.Start = context.Index;
            dateContext.Name = "Minute";

            dateContext.Minute = ParseDatePart(ref context, ref dateContext);
            ExpectColon(ref context, ref dateContext);

            dateContext.Second = ParseDateSeconds(ref context, ref dateContext);

            if (context.Current != '\'')
                ThrowTemporalError("Invalid time literal.", ref context, ref dateContext);

            ValidateTime(ref context, ref dateContext);

            context.Consume(); // '

            AddToken(SQLTokenType.TimeLiteral, start, ref context);
        }
        private static void ParseDatetime(ref SQLTokenizerContext context)
        {
            var start = context.Index;

            context.Consume(); // D/d
            context.Consume(); // T/t
            context.Consume(); // '

            // Initiate Year context
            var dateContext = new SQLTokenizerDateTimeContext(context.Index, 4, "Year", "YYYY-MM-DD HH:MM:SS[.fffffff]");

            dateContext.Year = ParseDatePart(ref context, ref dateContext);
            ExpectHyphen(ref context, ref dateContext);

            // Change to Month Context
            dateContext.Start = context.Index;
            dateContext.ExpectedDigit = 2;
            dateContext.Name = "Month";

            dateContext.Month = ParseDatePart(ref context, ref dateContext);
            ExpectHyphen(ref context, ref dateContext);

            // Change to Day Context
            dateContext.Start = context.Index;
            dateContext.Name = "Day";

            dateContext.Day = ParseDatePart(ref context, ref dateContext);

            if (context.Current != ' ')
                ThrowTemporalError("Invalid datetime literal.", ref context, ref dateContext);

            context.Consume();

            // Change to Hour Context
            dateContext.Start = context.Index;
            dateContext.Name = "Hour";

            dateContext.Hour = ParseDatePart(ref context, ref dateContext);
            ExpectColon(ref context, ref dateContext);

            // Change to Minute Context
            dateContext.Start = context.Index;
            dateContext.Name = "Minutes";

            dateContext.Minute = ParseDatePart(ref context, ref dateContext);
            ExpectColon(ref context, ref dateContext);

            // Change to Second Context
            dateContext.Start = context.Index;
            dateContext.Name = "Second";

            dateContext.Second = ParseDateSeconds(ref context, ref dateContext);

            if (context.Current != '\'')
                ThrowTemporalError("Invalid datetime literal.", ref context, ref dateContext);

            ValidateDate(ref context, ref dateContext);
            ValidateTime(ref context, ref dateContext);

            context.Consume(); // '

            AddToken(SQLTokenType.DatetimeLiteral, start, ref context);
        }
        private static void ParseHex(ref SQLTokenizerContext context)
        {
            var start = context.Index;

            context.Consume(); // Consume 0
            context.Consume(); // Consume x

            var digitStart = context.Index;

            while (context.Current != '\0' && IsHexDigit(context.Current))
                context.Consume();

            if (context.Index == digitStart)
            {
                var num = context.Span.Slice(start, context.Index - start);
                throw new FormatException(
                    ErrorFormatter.Create(
                        context.Span.ToString(),
                        context.Index,
                        $"Hex literal requires digits '{num}'"));
            }

            AddToken(SQLTokenType.HexLiteral, start, ref context);
        }
        private static void ParseString(ref SQLTokenizerContext context)
        {
            int start = context.Index;
            SQLTokenType type = SQLTokenType.StringLiteral;

            if (context.Current == 'n' || context.Current == 'N')
            {
                context.Consume(); // Consume n/N
                type = SQLTokenType.UnicodeString;
            }

            context.Consume(); // Consume opening quote

            while (context.Current != '\0')
            {
                if (context.Current == '\'')
                {
                    if (context.Peek() == '\'')
                    {
                        context.Consume(); // First quote
                        context.Consume(); // Second quote

                        continue;
                    }

                    context.Consume(); // Close quote
                    AddToken(type, start, ref context);
                    return;
                }
                context.Consume();
            }

            throw new FormatException(ErrorFormatter.Create(context.Span.ToString(), context.Index, "Unterminated string literal!"));
        }
        private static void ParseDelimitedIdentifier(char closingChar, ref SQLTokenizerContext context)
        {
            int start = context.Index;
            context.Consume(); // Opening quote/bracket

            while (context.Current != '\0')
            {
                if (context.Current == closingChar)
                {
                    if (context.Peek() == closingChar)
                    {
                        context.Consume(); // First quote/bracket
                        context.Consume(); // Second quote/bracket
                        continue;
                    }

                    context.Consume(); // Close quote/bracket
                    AddToken(
                        closingChar == ']'
                            ? SQLTokenType.BracketIdentifier
                            : SQLTokenType.DoubleQuoteIdentifier,
                        start,
                        ref context);

                    return;
                }
                context.Consume();
            }

            throw new FormatException(ErrorFormatter.Create(context.Span.ToString(), context.Index, $"Unterminated quoted identifier, missing '{closingChar}'"));
        }
        private static void ParseNumber(ref SQLTokenizerContext context)
        {
            var start = context.Index;
            var dotCount = 0;
            var hasDigit = false;
            SQLTokenType type = SQLTokenType.IntegerLiteral;

            if (context.Current == '$')
            {
                type = SQLTokenType.MoneyLiteral;
                context.Consume();
            }

            while (char.IsDigit(context.Current) || context.Current == '.')
            {
                if (char.IsDigit(context.Current) && !hasDigit)
                    hasDigit = true;

                if (context.Current == '.')
                {
                    dotCount++;

                    if (dotCount > 1)
                    {
                        var num = context.Span.Slice(start, context.Index - start);
                        var err = ErrorFormatter.Create(context.Span.ToString(), context.Index, $"Multiple decimal points '{num}'");
                        throw new FormatException(err);
                    }

                    if (type != SQLTokenType.MoneyLiteral)
                        type = SQLTokenType.DecimalLiteral;
                }

                context.Consume();
            }

            if (!hasDigit)
            {
                var num = context.Span.Slice(start, context.Index - start);
                var err = ErrorFormatter.Create(context.Span.ToString(), context.Index, $"Number requires digits '{num}'");
                throw new FormatException(err);
            }

            // Scientific notation
            if (context.Current == 'e' || context.Current == 'E')
            {
                context.Consume();

                if (context.Current == '+' || context.Current == '-')
                    context.Consume();


                if (!char.IsDigit(context.Current))
                {
                    var num = context.Span.Slice(start, context.Index - start);
                    var err = ErrorFormatter.Create(context.Span.ToString(), context.Index, $"Invalid scientific notation '{num}'");
                    throw new FormatException(err);
                }

                while (char.IsDigit(context.Current))
                    context.Consume();

                if (context.Current == 'e' || context.Current == 'E')
                {
                    var num = context.Span.Slice(start, context.Index - start);
                    var err = ErrorFormatter.Create(context.Span.ToString(), context.Index, $"Too many Exponent Marker '{num}'");
                    throw new FormatException(err);
                }

                if (context.Current == '.')
                {
                    var num = context.Span.Slice(start, context.Index - start);
                    var err = ErrorFormatter.Create(context.Span.ToString(), context.Index, $"Exponent must be integer '{num}'");
                    throw new FormatException(err);
                }

                type = SQLTokenType.ScientificLiteral;
            }

            AddToken(type, start, ref context);
        }
        private static void ParseIdentifierOrKeyword(ref SQLTokenizerContext context)
        {
            var start = context.Index;
            SQLTokenType type = SQLTokenType.Identifier;

            if (context.Current == '@')
            {
                context.Consume();
                if (context.Current == '@')
                    context.Consume();
                type = SQLTokenType.Variable;
            }

            while (context.Current != '\0' && (char.IsLetterOrDigit(context.Current) || context.Current == '_'))
            {
                context.Consume();
            }

            var text = context.Span.Slice(start, context.Index - start);

            if (text.Equals("NULL", StringComparison.OrdinalIgnoreCase))
                type = SQLTokenType.NullLiteral;
            else if (SQLDictionary.SQLKeywordsLookup.Contains(text))
                type = SQLTokenType.Keyword;
            else if (SQLDictionary.SQLCustomKeywordsLookup.Contains(text))
                type = SQLTokenType.Keyword;
            else if (SQLDictionary.SQLCustomDataTypeLookup.Contains(text))
                type = SQLTokenType.SQLType;

            AddToken(type, start, ref context);
        }
        private static void ConsumeAndAddToken(SQLTokenType type, int amount, ref SQLTokenizerContext context)
        {
            var start = context.Index;

            if (amount < 0 || amount > context.Span.Length - context.Index)
                throw new ArgumentOutOfRangeException(nameof(amount));

            context.AdvanceIndex(amount);

            AddToken(type, start, ref context);
        }
        private static int ParseDatePart(ref SQLTokenizerContext context, ref SQLTokenizerDateTimeContext dateContext)
        {
            // 1. Ensure have enough characters left in the input to even read expectedDigits
            if (context.Index + dateContext.ExpectedDigit > context.Span.Length)
                ThrowTemporalError($"Invalid {dateContext.Name}", ref context, ref dateContext);

            // 2. Slice exact target window zero-allocation
            ReadOnlySpan<char> partSpan = context.Span.Slice(context.Index, dateContext.ExpectedDigit);

            // 3. Fast-parse integer from span (implicitly validates digit format)
            int value = 0;

            foreach (char c in partSpan)
            {
                if (c < '0' || c > '9')
                {
                    ThrowTemporalError(
                        $"Invalid {dateContext.Name} {partSpan}",
                        ref context,
                        ref dateContext);
                }

                value = value * 10 + (c - '0');
            }
            // Advance position by expectedDigits
            context.AdvanceIndex(dateContext.ExpectedDigit);

            return value;
        }
        private static decimal ParseDateSeconds(ref SQLTokenizerContext context, ref SQLTokenizerDateTimeContext dateContext)
        {
            int partStart = context.Index;

            // 1. Advance the lexer pointer past all valid second characters (digits and dot)
            while (context.Current != '\0' && (char.IsDigit(context.Current) || context.Current == '.'))
            {
                context.Consume();
            }

            // 2. Extract zero-allocation slice of the seconds segment
            ReadOnlySpan<char> secondsSpan = context.Span.Slice(partStart, context.Index - partStart);

            if (secondsSpan.IsEmpty)
            {
                ThrowTemporalError("Invalid seconds.", ref context, ref dateContext);
            }

            // 3. Fast structural validation on the span
            int dotIndex = secondsSpan.IndexOf('.');
            if (dotIndex != -1)
            {
                // Check for multiple decimal points
                if (secondsSpan[(dotIndex + 1)..].Contains('.'))
                {
                    ThrowTemporalError("Multiple decimal points.", ref context, ref dateContext);
                }

                // Check fractional digit count (max 7 digits)
                int fractionalCount = secondsSpan.Length - (dotIndex + 1);
                if (fractionalCount > 7)
                {
                    ThrowTemporalError("Seconds cannot have more than 7 fractional digits.", ref context, ref dateContext);
                }
            }

            // 4. Single-pass parse directly off the span
            if (!decimal.TryParse(secondsSpan, CultureInfo.InvariantCulture, out decimal value))
            {
                ThrowTemporalError("Invalid seconds.", ref context, ref dateContext);
            }

            return value;
        }

        #endregion
        #region Helper
        private static bool IsHexDigit(char c) =>
            (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
        private static void AddToken(SQLTokenType type, int start, ref SQLTokenizerContext context)
        {
            context.AddToken(new SQLToken(
                type,
                start,
                context.Index - start));
        }
        private static void ThrowTemporalError(string message, ref SQLTokenizerContext context, ref SQLTokenizerDateTimeContext dateContext)
        {
            var err = ErrorFormatter.Create(
                context.Span.ToString(),
                dateContext.Start,
                $"{message}. Expected format: {dateContext.Format}.");
            throw new FormatException(err);
        }
        private static void ExpectHyphen(ref SQLTokenizerContext context, ref SQLTokenizerDateTimeContext dateContext)
        {
            if (context.Current != '-')
                ThrowTemporalError("Expected '-' in date.", ref context, ref dateContext);

            context.Consume();
        }
        private static void ExpectColon(ref SQLTokenizerContext context, ref SQLTokenizerDateTimeContext dateContext)
        {
            if (context.Current != ':')
                ThrowTemporalError("Expected ':' in time.", ref context, ref dateContext);

            context.Consume();
        }
        private static void ValidateTime(ref SQLTokenizerContext context, ref SQLTokenizerDateTimeContext dateContext)
        {
            var format = dateContext.Format;

            if (dateContext.Hour is < 0 or > 23)
                ThrowTemporalError($"Invalid hours {dateContext.Hour}.", ref context, ref dateContext);

            if (dateContext.Minute is < 0 or > 59)
                ThrowTemporalError($"Invalid minutes {dateContext.Minute}.", ref context, ref dateContext);

            if (dateContext.Second < 0 || dateContext.Second >= 60)
                ThrowTemporalError($"Invalid seconds {dateContext.Second}.", ref context, ref dateContext);
        }
        private static void ValidateDate(ref SQLTokenizerContext context, ref SQLTokenizerDateTimeContext dateContext)
        {
            var month = dateContext.Month;
            if (month is < 1 or > 12)
                ThrowTemporalError($"Invalid month {month}.", ref context, ref dateContext);

            int maxDay = SQLDictionary.DaysInMonth[month - 1];

            var day = dateContext.Day;
            if (day is < 1 || day > maxDay)
                ThrowTemporalError($"Invalid day {day}.", ref context, ref dateContext);
        }
        #endregion
    }
}
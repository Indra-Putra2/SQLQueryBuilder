using SQLQueryBuilder.Enum;
using SQLQueryBuilder.Model;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.RegularExpressions;

namespace SQLQueryBuilder.SQLBuilder.SELECT_Parser
{
    internal class SQLSelectParser2
    {
        private string _input;
        private List<SelectToken> _tokens;
        private int _index = 0;
        private ColumnMetadataModel _currentMetadata;
        private List<ColumnMetadataModel> _columns;

        private static readonly HashSet<SelectTokenType> _identifiers = [
            SelectTokenType.Identifier,
            SelectTokenType.BracketIdentifier,
            SelectTokenType.DoubleQuoteIdentifier,
            SelectTokenType.BacktickIdentifier,
        ];
        private static readonly HashSet<SelectTokenType> _quotedOrBracketedIdentifier = [
            SelectTokenType.BracketIdentifier,
            SelectTokenType.DoubleQuoteIdentifier,
            SelectTokenType.BacktickIdentifier,
        ];
        private static readonly HashSet<SelectTokenType> _arithmetic = [
            SelectTokenType.Plus,
            SelectTokenType.Minus,
            SelectTokenType.Divide,
            SelectTokenType.Modulo,
            SelectTokenType.Asterisk
        ];
        private static readonly HashSet<SelectTokenType> _literals = [
            SelectTokenType.Number,
            SelectTokenType.String
        ];
        private static readonly HashSet<SelectTokenType> _delimitersAndTerminators = [
            SelectTokenType.Comma,
            SelectTokenType.EOF
        ];
        private static readonly HashSet<SelectTokenType> _expressionTerminators = [
            .._delimitersAndTerminators,
            .._identifiers,
            SelectTokenType.String,
            SelectTokenType.Keyword
        ];

        private SelectToken Current =>
            _index < _tokens.Count ? _tokens[_index] : _tokens[^1];

        private SelectToken Peek(int offset = 1) =>
            _index + offset < _tokens.Count ? _tokens[_index + offset] : _tokens[^1];

        private SelectToken Consume() =>
            _index < _tokens.Count ? _tokens[_index++] : _tokens[^1];

        public List<ColumnMetadataModel> Parse(string input, List<SelectToken> tokens)
        {
            if (IsStringNullOrEmpty(input)) throw new InvalidDataException("Input Cannot be NULL or empty!");

            _input = Regex.Replace(input, @"\s+|\n", " ");
            _tokens = tokens;
            _index = 0;
            var columns = new List<ColumnMetadataModel>();

            while (Current.Type != SelectTokenType.EOF)
            {
                var metadata = CreateMetadata(ParseExpression());

                // Handle optional aliases (e.g., AS MyAlias or just MyAlias)
                if (IsKeyword(Current, "AS") || Expect(Current, SelectTokenType.String))
                {
                    metadata.OriginalQuery += $" {Consume().Value}"; // Eat 'AS' 
                    if ((!Expects(Current, _identifiers) && !Expect(Current, SelectTokenType.String)) || IsFunction(Current, Peek()))
                    {
                        var err = CreateErrorMessage(_input, Current.StartPosition, $"Alias with the value {Current.Value} ({Current.Type}) is not a valid alias!", errorTextLength: Current.length);
                        throw new InvalidDataException(err);
                    }
                    var alias = Consume();
                    var errorMessage = CreateErrorMessage(_input, alias.StartPosition, $"Alias cannot be null or empty!", errorTextLength: alias.length);

                    metadata.OriginalQuery += $" {alias.Value}";
                    metadata.Alias = CleanAndValidate(alias.Value, errorMessage);
                }
                else if (Expects(Current, _identifiers) || Expect(Current, SelectTokenType.String))
                {
                    if (IsFunction(Current, Peek()))
                    {
                        var err = CreateErrorMessage(_input, Current.StartPosition, $"Alias with the value {Current.Value} ({Current.Type}) is not a valid alias!", errorTextLength: Current.length);
                        throw new InvalidDataException(err);
                    }
                    var alias = Consume();
                    var errorMessage = CreateErrorMessage(_input, alias.StartPosition, $"Alias cannot be null or empty!", errorTextLength: alias.length);

                    metadata.OriginalQuery += $" {alias.Value}";
                    metadata.Alias = CleanAndValidate(alias.Value, errorMessage);
                }

                columns.Add(metadata);

                // Handle commas between columns
                if (Expect(Current, SelectTokenType.Comma))
                {
                    Consume();
                    if (Expect(Current, SelectTokenType.EOF))
                    {
                        var errorMessage = CreateErrorMessage(_input, Current.StartPosition, $"Expected Identifier but got: \"{Current.Value}\" ({Current.Type})", errorTextLength: Current.length);
                        throw new InvalidDataException(errorMessage);
                    }
                }
                else if (!Expect(Current, SelectTokenType.EOF))
                {
                    var errorMessage = CreateErrorMessage(_input, Current.StartPosition, $"Expected comma or EOF, but got: \"{Current.Value}\" ({Current.Type})", errorTextLength: Current.length);
                    throw new InvalidDataException(errorMessage);
                }
            }

            return columns;
        }

        #region Data Extraction
        private (string tableAlias, string columnName, string queryText, bool isExpression) ParseExpression()
        {
            StringBuilder sb = new StringBuilder();
            string initialTableAlias = null;
            string initialColumnName = null;
            bool isExpression = false;

            var primary = ParsePrimary();
            sb.Append(primary.text).Append(" ");
            initialTableAlias = primary.tableAlias;
            initialColumnName = primary.columnName;
            isExpression = primary.isExpression;

            while (Expects(Current, _arithmetic))
            {
                isExpression = true;
                initialTableAlias = null; // Blended data source means single table alias is null

                var op = Consume().Value; // Extract the operator (+, -, *, /)
                sb.Append(op).Append(" ");

                var nextPrimary = ParsePrimary();
                sb.Append(nextPrimary.text).Append(" ");
            }

            string finalResult = sb.ToString().Trim();

            // If it's a compound expression, the column name is the full calculation string
            string finalColumnName = isExpression ? finalResult : initialColumnName;

            return (initialTableAlias, finalColumnName, finalResult, isExpression);
        }

        private (string text, string tableAlias, string columnName, bool isExpression) ParsePrimary()
        {
            if (IsFunction(Current, Peek())) // e.g., COUNT(*)
            {
                var funcText = ParseFunction();
                return (funcText, null, funcText, true);
            }
            else if (IsKeyword(Current, "CASE"))
            {
                var caseText = ParseCASE();
                return (caseText, null, caseText, true);
            }
            else if (IsQualifiedColumn(Current, Peek())) // e.g., r.name
            {
                var tableToken = Consume();
                var tableError = CreateErrorMessage(_input, tableToken.StartPosition, "Table at index cannot be null or empty!", errorTextLength: tableToken.length);
                var finalTable = CleanAndValidate(tableToken.Value, tableError);

                Consume(); // Eat '.'
                Expects(Current, _identifiers); // Make sure its identifiers

                var colToken = Consume();
                var columnError = CreateErrorMessage(_input, colToken.StartPosition, "Column at index cannot be null or empty!", errorTextLength: colToken.length);
                var finalCol = CleanAndValidate(colToken.Value, columnError);

                return ($"{tableToken.Value}.{colToken.Value}", finalTable, finalCol, false);
            }
            else if (Expects(Current, _identifiers)) //e.g., name
            {
                var colToken = Consume();
                var columnError = CreateErrorMessage(_input, colToken.StartPosition, "Column at index cannot be null or empty!", errorTextLength: colToken.length);
                var finalCol = CleanAndValidate(colToken.Value, columnError);
                return (colToken.Value, null, finalCol, false);
            }
            else if (Expect(Current, SelectTokenType.String)) //e.g., 'name'
            {
                var str = Consume().Value;
                var finalSTR = RemoveQuoteOrBracket(str);
                return (str, null, finalSTR, true);
            }
            else if (Expect(Current, SelectTokenType.Number)) //e.g., 123
            {
                var num = Consume().Value;
                return (num, null, num, true);
            }
            var errorMessage = CreateErrorMessage(_input, Current.StartPosition, $"Unexpected token in expression: \"{Current.Value}\" ({Current.Type})", errorTextLength: Current.length);
            throw new InvalidDataException(errorMessage);
        }

        private string ParseCASE()
        {
            StringBuilder sb = new StringBuilder();
            var startToken = Consume(); // Consume "CASE"
            sb.Append(startToken.Value).Append(" ");

            const int ExpectWhen = 0;
            const int ExpectThen = 1;
            const int ExpectClause = 2;

            int state = ExpectWhen;

            bool seenWhen = false;

            //Simple CASE CASE <expression> WHEN .... 
            if (!IsKeyword(Current, "WHEN"))
            {
                var baseExpr = ParseExpression();
                sb.Append(baseExpr.queryText).Append(" ");
            }

            // Loop through structural segments of the CASE block
            while (!IsKeyword(Current, "END") && !Expect(Current, SelectTokenType.EOF))
            {
                switch (state)
                {
                    case ExpectWhen:

                        if (!IsKeyword(Current, "WHEN"))
                        {
                            throw new InvalidDataException(CreateErrorMessage(_input, Current.StartPosition,"Expected WHEN clause.", errorTextLength:Current.length));
                        }

                        seenWhen = true;

                        sb.Append(Consume().Value).Append(" "); // WHEN
                        sb.Append(ParseExpression().queryText).Append(" ");
                        state = ExpectThen;

                    break;

                    case ExpectThen:

                        if (!IsKeyword(Current, "THEN"))
                        {
                            throw new InvalidDataException(CreateErrorMessage(_input, Current.StartPosition, "Expected THEN after WHEN expression.", errorTextLength: Current.length));
                        }

                        sb.Append(Consume().Value).Append(" "); // THEN
                        sb.Append(ParseExpression().queryText).Append(" ");

                        state = ExpectClause;

                    break;

                    case ExpectClause:

                        if (IsKeyword(Current, "WHEN"))
                        {
                            state = ExpectWhen;
                        }
                        else if (IsKeyword(Current, "ELSE"))
                        {
                            sb.Append(Consume().Value).Append(" "); // ELSE
                            sb.Append(ParseExpression().queryText).Append(" ");

                            if (!IsKeyword(Current, "END"))
                            {
                                throw new InvalidDataException(
                                    CreateErrorMessage(
                                        _input,
                                        Current.StartPosition,
                                        "Expected END after ELSE clause.",
                                        Current.length));
                            }
                            break;
                        }
                        else
                        {
                            throw new InvalidDataException(CreateErrorMessage(_input, Current.StartPosition, "Expected WHEN, ELSE, or END.", errorTextLength: Current.length));
                        }

                    break;
                }
                
            }

            if (!seenWhen)
            {
                throw new InvalidDataException(CreateErrorMessage(_input, startToken.StartPosition, "Expected at least one WHEN clause in CASE expression.", startToken.length));
            }

            if (!IsKeyword(Current, "END"))
            {
                throw new InvalidDataException(CreateErrorMessage(_input, Current.StartPosition, "Missing closing END for CASE call.", startToken.length));
            }

            sb.Append(Consume().Value); // Consume closing "END"
            return sb.ToString();
        }
        private string ParseFunction()
        {
            StringBuilder sb = new StringBuilder();

            // 1. Consume function name (e.g., "ROUND")
            sb.Append(Consume().Value);

            var startIndex = Current.StartPosition;

            // 2. Consume opening "("
            if (!Expect(Current, SelectTokenType.LeftParen))
            {
                var err = CreateErrorMessage(_input, Current.StartPosition, "Expected opening parenthesis for function call.", Current.length);
                throw new InvalidDataException(err);
            }
            sb.Append(Consume().Value);

            // 3. Recursively parse arguments separated by commas
            while (!Expect(Current, SelectTokenType.RightParen) && !Expect(Current, SelectTokenType.EOF))
            {
                // Recursion Magic: Parse whatever expression is inside the argument slot
                var argument = ParseExpression();
                sb.Append(argument.queryText);

                if (Expect(Current, SelectTokenType.Comma))
                {
                    sb.Append(Consume().Value).Append(" "); // Eat comma and add space
                }
            }

            // 4. Verify and consume closing ")"
            if (!Expect(Current, SelectTokenType.RightParen))
            {
                throw new InvalidDataException(CreateErrorMessage(_input, startIndex, "Missing closing parenthesis for function call."));
            }

            sb.Append(Consume().Value); // Consume closing ")"
            return sb.ToString();
        }
        private string? RemoveQuoteOrBracket(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var pattern = @"\[|\]|\""|\`|\'";
            return Regex.Replace(input, pattern, "");
        }
        private string CleanAndValidate(string input, string message)
        {
            var result = RemoveQuoteOrBracket(input);
            if (IsStringNullOrEmpty(result))
                throw new InvalidDataException(message);
            return result;
        }
        #endregion

        #region Helpers Methods
        private bool IsStringNullOrEmpty(string input)
        {
            return string.IsNullOrWhiteSpace(input);
        }
        private bool IsQualifiedColumn(SelectToken currentToken, SelectToken tokenAfter)
        {
            return Expect(currentToken, SelectTokenType.Identifier) && Expect(tokenAfter, SelectTokenType.Dot);
        }
        private bool IsFunction(SelectToken currentToken, SelectToken tokenAfter)
        {
            return Expects(currentToken, _identifiers) && Expect(tokenAfter, SelectTokenType.LeftParen);
        }
        private bool IsKeyword(SelectToken token, string Keyword)
        {
            return Expect(token, SelectTokenType.Keyword) && string.Equals(token.Value, Keyword, StringComparison.InvariantCultureIgnoreCase);
        }
        private ColumnMetadataModel CreateMetadata((string tableAlias, string columnName, string queryText, bool isExpression) exp)
        {
            return new ColumnMetadataModel()
            {
                OriginalQuery = exp.queryText.Trim(),
                TableAlias = exp.tableAlias,
                ColumnName = exp.columnName,
                Alias = null,
                IsExpression = exp.isExpression,
            };
        }
        private bool Expect(SelectToken current, SelectTokenType expectedToken)
        {
            return current.Type == expectedToken;
        }
        private bool Expects(SelectToken current, HashSet<SelectTokenType> expectedTokens)
        {
            return expectedTokens.Contains(current.Type);
        }
        private (string Snippet, int Pointer, int Length, int Start, int End) GetContext(string text, int startIdx, int endIdx, int contextLength)
        {
            int start = Math.Max(0, startIdx - contextLength);
            int end = Math.Min(text.Length, endIdx + contextLength + 1);
            int length = end - start;
            return (text[start..end], startIdx - start, length, start, end);
        }

        private string CreateErrorMessage(string text, int errorIndexPos, string errorMessage, int errorTextLength = 1, int contextLength = 25)
        {
            // 1. Force the length to be at least 1
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
                .Append("\n")
                .Append($"Error At Position: {errorIndexPos}")
                .Append("\n")
                .Append(enclosing)
                .Append("\n")
                .Append(leftDots)
                .Append(context.Snippet)
                .Append(rightDots)
                .Append("\n")
                .Append(pointer)
                .Append("\n")
                .Append(enclosing)
                .Append("\n")
                .Append(errorMessage).ToString();
        }
        #endregion
    }
}
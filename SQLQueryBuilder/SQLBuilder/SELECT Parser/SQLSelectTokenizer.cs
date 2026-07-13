using SQLQueryBuilder.Enum;
using SQLQueryBuilder.Model;
using System.Text;
using System.Text.RegularExpressions;

namespace SQLQueryBuilder.SQLBuilder.SELECT_Parser
{
    internal class SQLSelectTokenizer
    {
        private List<SelectToken> _tokens;
        private string _sql = string.Empty;
        private int _index = 0;
        private static readonly HashSet<string> _sqlKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            // Core
            "SELECT", "FROM", "WHERE", "INSERT", "UPDATE", "DELETE",

            // Clauses
            "INTO", "VALUES", "SET",

            // Joins
            "JOIN", "INNER", "LEFT", "RIGHT", "FULL", "OUTER", "ON",

            // Logic
            "AND", "OR", "NOT", "IN", "EXISTS", "BETWEEN", "LIKE", "IS", "NULL",

            // Sorting / grouping
            "ORDER", "BY", "GROUP", "HAVING", "DISTINCT",

            // Aliasing / flow
            "AS",

            // Control flow
            "CASE", "WHEN", "THEN", "ELSE", "END",

            // Schema
            "CREATE", "ALTER", "DROP", "TABLE", "VIEW", "INDEX",

            // Extras
            "LIMIT", "OFFSET"
        };
        private char Current =>
            _index < _sql.Length
                ? _sql[_index]
                : '\0';

        private char Peek(int offset = 1) =>
            _index + offset < _sql.Length
                ? _sql[_index + offset]
                : '\0';

        private char Consume() =>
            _index < _sql.Length
                ? _sql[_index++]
                : '\0';

        public List<SelectToken> Parse(string Select)
        {
            if(string.IsNullOrWhiteSpace(Select)) throw new ArgumentNullException("Select cannot be NULL or empty!");

            _tokens = new();
            _sql = Regex.Replace(Select, @"\s+|\n", " ");
            _index = 0;

            while (Current != '\0')
            {
                var c = Current;

                if (char.IsWhiteSpace(c))
                {
                    Consume();
                    continue;
                }
                else if (char.IsLetter(c) || c == '_')
                {
                    ParseIdentifierOrKeyword();
                }
                else if (char.IsDigit(c))
                {
                    ParseNumber();
                }
                else if (c == '[')
                {
                    ParseQuotedIdentifier(']', SelectTokenType.BracketIdentifier);
                }
                else if (c == '"')
                {
                    ParseQuotedIdentifier('"', SelectTokenType.DoubleQuoteIdentifier);
                }
                else if (c == '`')
                {
                    ParseQuotedIdentifier('`', SelectTokenType.BacktickIdentifier);
                }
                else if (c == '\'')
                {
                    ParseString();
                }
                else if (c == '.')
                {
                    var start = _index;
                    AddToken(SelectTokenType.Dot, Consume().ToString(), start, 1);
                }
                else if (c == ',')
                {
                    var start = _index;
                    AddToken(SelectTokenType.Comma, Consume().ToString(), start, 1);
                }
                else if (c == ';')
                {
                    var start = _index;
                    AddToken(SelectTokenType.Semicolon, Consume().ToString(), start, 1);
                }
                else if (c == '(')
                {
                    var start = _index;
                    AddToken(SelectTokenType.LeftParen, Consume().ToString(), start, 1);
                }
                else if (c == ')')
                {
                    var start = _index;
                    AddToken(SelectTokenType.RightParen, Consume().ToString(), start, 1);
                }
                else if (c == '+')
                {
                    var start = _index;
                    AddToken(SelectTokenType.Plus, Consume().ToString(), start, 1);
                }
                else if (c == '-')
                {
                    var start = _index;
                    AddToken(SelectTokenType.Minus, Consume().ToString(), start, 1);
                }
                else if (c == '/')
                {
                    var start = _index;
                    AddToken(SelectTokenType.Divide, Consume().ToString(), start, 1);
                }
                else if (c == '*')
                {
                    var start = _index;
                    AddToken(SelectTokenType.Asterisk, Consume().ToString(), start, 1);
                }
                else if (c == '%')
                {
                    var start = _index;
                    AddToken(SelectTokenType.Modulo, Consume().ToString(), start, 1);
                }
                else if (c == '=')
                {
                    var start = _index;
                    AddToken(SelectTokenType.Equal, Consume().ToString(), start, 1);
                }
                else if (c == '<')
                {
                    var start = _index;
                    if (Peek() == '=')
                    {
                        Consume(); // Consume '<'
                        Consume(); // Consume '='
                        AddToken(SelectTokenType.LessThanOrEqual, "<=", start, 2);
                    }
                    else if (Peek() == '>')
                    {
                        Consume(); // Consume '<'
                        Consume(); // Consume '>'
                        AddToken(SelectTokenType.NotEqual, "<>", start, 2);
                    }
                    else
                    {
                        Consume(); // Consume '<'
                        AddToken(SelectTokenType.LessThan, "<", start, 1);
                    }
                }
                else if (c == '>')
                {
                    var start = _index;
                    if (Peek() == '=')
                    {
                        Consume(); // Consume '>'
                        Consume(); // Consume '='
                        AddToken(SelectTokenType.GreaterThanOrEqual, ">=", start, 2);
                    }
                    else
                    {
                        Consume(); // Consume '>'
                        AddToken(SelectTokenType.GreaterThan, ">", start, 1);
                    }
                }
                else if (c == '!')
                {
                    var start = _index;
                    if (Peek() == '=')
                    {
                        Consume(); // Consume '!'
                        Consume(); // Consume '='
                        AddToken(SelectTokenType.NotEqual, "!=", start, 2);
                    }
                    else
                    {
                        Consume(); // Consume '!'
                        AddToken(SelectTokenType.Unknown, "!", start, 1);
                    }
                }
                else
                {
                    var start = _index;
                    AddToken(SelectTokenType.Unknown, Consume().ToString(), start, 1);
                }
            }
            AddToken(SelectTokenType.EOF, "EOF", _sql.Length, 3);
            return _tokens;
        }

        //Methods
        private void ParseString()
        {
            var sb = new StringBuilder();
            int startIndex = _index;
            sb.Append(Consume()); // Consume the opening single quote '\''

            while (Current != '\0')
            {
                if (Current == '\'')
                {
                    if (Peek() == '\'')
                    {
                        sb.Append('\''); // Add a single literal quote to the string value
                        Consume();       // Consume first quote
                        Consume();       // Consume second quote
                        continue;
                    }

                    sb.Append(Consume()); // Consume the closing quote
                    int totalLength = _index - startIndex;
                    AddToken(SelectTokenType.String, sb.ToString(), startIndex, totalLength);
                    return; // Exit the method completely
                }

                sb.Append(Consume());
            }

            // If we hit '\0' without returning, the string was never closed
            throw new Exception($"Unterminated string literal starting at index {startIndex}");
        }

        private void ParseQuotedIdentifier(char closingChar, SelectTokenType tokenType)
        {
            var sb = new StringBuilder();
            int startIndex = _index;
            sb.Append(Consume()); // Consume opening bracket/quote

            while (Current != '\0')
            {
                // If we hit the closing character, check if it's an escaped pair
                if (Current == closingChar)
                {
                    // Look ahead to see if the next char is ALSO the closing character
                    if (Peek() == closingChar)
                    {
                        sb.Append(closingChar); // Add exactly ONE of them to the value
                        Consume();              // Consume the first closing character
                        Consume();              // Consume the second closing character
                        continue;               // Keep parsing the identifier
                    }

                    // If it's NOT doubled up, we found the true end!
                    sb.Append(Consume()); // Consume the final closing character
                    int totalLength = _index - startIndex;
                    AddToken(tokenType, sb.ToString(), startIndex, totalLength);
                    return;
                }

                // Otherwise, it's just a normal character inside the quotes
                sb.Append(Consume());
            }

            // If the loop exits without hitting 'return', the identifier never closed
            throw new Exception($"Unterminated quoted identifier starting at index {startIndex}");
        }

        private void ParseNumber()
        {
            var sb = new StringBuilder();
            var start = _index;
            var dotAmount = 0;

            while (Current != '\0' && (char.IsDigit(Current) || Current == '.'))
            {
                if (Current == '.') dotAmount++;
                if (dotAmount > 1)
                {
                    sb.Append(Consume());
                    throw new InvalidDataException($"Invalid Data {sb.ToString()} has one too many decimals!");
                }
                sb.Append(Consume());
            }

            AddToken(SelectTokenType.Number, sb.ToString(), start, sb.ToString().Length);
        }

        private void ParseIdentifierOrKeyword()
        {
            var sb = new StringBuilder();
            var start = _index;

            while (Current != '\0' && (char.IsLetterOrDigit(Current) || Current == '_'))
            {
                sb.Append(Consume());
            }

            var value = sb.ToString();
            var lower = value.ToLowerInvariant();
            bool isSqlKeyword = _sqlKeywords.Contains(lower);

            if (isSqlKeyword)
            {
                AddToken(SelectTokenType.Keyword, sb.ToString(), start, sb.ToString().Length);
            }
            else
            {
                AddToken(SelectTokenType.Identifier, sb.ToString(), start, sb.ToString().Length);
            }
        }

        private void AddToken(SelectTokenType type, string value, int start, int length)
        {
            var token = new SelectToken(type, value, start, length);
            _tokens.Add(token);
        }
    }
}

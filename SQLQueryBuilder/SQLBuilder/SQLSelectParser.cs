using SQLQueryBuilder.Model;
using System.Text;
using System.Text.RegularExpressions;

namespace SQLQueryBuilder.Builder
{
    internal class SQLSelectParser
    {
        private string _input = string.Empty;
        private int _index = 0;
        private List<ColumnMetadataModel> _columns;

        private char Peek() => IsAtEnd() ? '\0' : _input[_index];
        private char Consume() => IsAtEnd() ? throw new Exception($"Consume past end at Index={_index}") : _input[_index++];
        private bool IsAtEnd() => _index >= _input.Length;

        public List<ColumnMetadataModel> Parse(string input)
        {
            Init(input);
            var result = new StringBuilder();

            while (!IsAtEnd())
            {
                var c = Peek();
                if (c == '\'' || c == '"') result.Append(ParseQuote());
                else if (c == '(') result.Append(ParseParenthesis());
                else if (CASECheck()) result.Append(ParseCase());
                else if (c == ',')
                {
                    Consume(); // Eat the comma
                    ProcessAndAdd(result.ToString());
                    result.Clear();
                }
                else
                {
                    result.Append(Consume());
                }
            }
            if (result.Length > 0) ProcessAndAdd(result.ToString());

            return _columns;
        }

        private string ParseQuote()
        {
            char quoteChar = Peek();
            if (quoteChar != '"' && quoteChar != '\'')
                throw new InvalidOperationException($"Expected a quote but got {Peek()} instead! at index:{_index}");

            var start = _index;
            var result = new StringBuilder();

            result.Append(Consume());   // Consume opening quote

            while (!IsAtEnd() && Peek() != quoteChar)
            {
                result.Append(Consume());
            }

            if (Peek() != quoteChar)
                throw new InvalidOperationException($"Missing closing quote! starting at {start}\n{_input.Substring(start)}");

            result.Append(Consume()); // Consume closing quote

            return result.ToString();
        }

        private string ParseParenthesis()
        {
            if (Peek() != '(')
                throw new InvalidOperationException($"Expected a parenthesis but got {Peek()} instead! at index:{_index}");

            var start = _index;
            var result = new StringBuilder();
            var depth = 0;

            // Consume the initial opening parenthesis
            depth++;
            result.Append(Consume());

            while (!IsAtEnd() && depth > 0)
            {
                char c = Peek();
                if (c == '(')
                {
                    depth++;
                    result.Append(Consume());
                }
                else if (c == ')')
                {
                    depth--;
                    result.Append(Consume());
                }
                else if (c == '"' || c == '\'')
                {
                    result.Append(ParseQuote());
                }
                else if (CASECheck())
                {
                    result.Append(ParseCase());
                }
                else
                {
                    result.Append(Consume());
                }
            }

            if (depth != 0)
                throw new InvalidOperationException($"Missing parenthesis starting from index {start}\n{_input.Substring(start)}");

            return result.ToString();
        }

        private string ParseCase()
        {
            if (CASECheck())
            {
                var start = _index;
                var result = new StringBuilder();

                result.Append(Consume()); // C
                result.Append(Consume()); // A
                result.Append(Consume()); // S
                result.Append(Consume()); // E

                while (!IsAtEnd() && !ENDCheck())
                {
                    char c = Peek();
                    if (c == '(')
                    {
                        result.Append(ParseParenthesis());
                    }
                    else if (c == '"' || c == '\'')
                    {
                        result.Append(ParseQuote());
                    }
                    else if (CASECheck())
                    {
                        result.Append(ParseCase());
                    }
                    else
                    {
                        result.Append(Consume());
                    }
                }

                if (IsAtEnd())
                    throw new InvalidOperationException($"Missing closing CASE earlier with END! starting from {start}\n{_input.Substring(start)}");

                result.Append(Consume()); // E
                result.Append(Consume()); // N
                result.Append(Consume()); // D

                return result.ToString();
            }
            throw new InvalidOperationException($"Expected CASE but got {Peek()} instead! at index:{_index}");
        }

        private void Init(string input)
        {
            _input = input;
            _index = 0;
            _columns = new List<ColumnMetadataModel>();
        }

        private bool CASECheck()
        {
            if (_index + 3 < _input.Length)
            {
                if (_input[_index] == 'C' && _input[_index + 1] == 'A' && _input[_index + 2] == 'S' && _input[_index + 3] == 'E')
                {
                    if (_index + 4 >= _input.Length || char.IsWhiteSpace(_input[_index + 4]) || _input[_index + 4] == '(')
                        return true;
                }
            }
            return false;
        }

        private bool ENDCheck()
        {
            if (_index + 2 < _input.Length)
            {
                if (_input[_index] == 'E' && _input[_index + 1] == 'N' && _input[_index + 2] == 'D')
                {
                    if (_index + 3 >= _input.Length || char.IsWhiteSpace(_input[_index + 3]) || _input[_index + 3] == ')' || _input[_index + 3] == ',')
                        return true;
                }
            }
            return false;
        }

        private void ProcessAndAdd(string rawChunk)
        {
            string trimmed = Regex.Replace(rawChunk.Trim(), @"\s+", " ");
            if (string.IsNullOrEmpty(trimmed)) return;

            // If it ends with a parenthesis, it's a function call with NO alias
            if (trimmed.EndsWith(')'))
            {
                var columnData = new ColumnMetadataModel()
                {
                    OriginalQuery = trimmed,
                    TableAlias = null,
                    ColumnName = trimmed,
                    Alias = null,
                    IsExpression = true
                };

                _columns.Add(columnData);
                return;
            }

            int i = trimmed.Length - 1;
            char lastChar = trimmed[i];

            // Case 1: The alias is wrapped in quotes or brackets
            if (lastChar == '"' || lastChar == '\'' || lastChar == ']' || lastChar == '`')
            {
                char targetOpen = lastChar == ']' ? '[' : lastChar;
                i--; // Skip past the closing boundary marker

                // Loop backward exclusively to find the matching opening wrapper character
                while (i >= 0 && trimmed[i] != targetOpen)
                {
                    i--;
                }

                // 'i' now points directly to the opening quote character ('"', '\'', or '[')
                string alias = trimmed.Substring(i).Trim();
                string expression = trimmed.Substring(0, i).TrimEnd();

                // Strip off the optional "AS" keyword if present
                if (expression.EndsWith(" AS", StringComparison.OrdinalIgnoreCase))
                {
                    expression = expression.Substring(0, expression.Length - 3).TrimEnd();
                }

                // Clean up structural quotes for the output representation
                string cleanAlias = alias.Substring(1, alias.Length - 2);
                
                var columnData = new ColumnMetadataModel()
                {
                    OriginalQuery = trimmed,
                    TableAlias = null,
                    ColumnName = expression,
                    Alias = cleanAlias,
                    IsExpression = true
                };

                _columns.Add(columnData);
                return;
            }

            // Case 2: Standard, plain alias without spaces (e.g., total_orders)
            // Just track backward until you hit the space boundary separating column from alias
            while (i >= 0 && trimmed[i] != ' ')
            {
                i--;
            }

            if (i != -1)
            {
                string alias = trimmed.Substring(i + 1).Trim();
                string expression = trimmed.Substring(0, i).TrimEnd();
                // check if the left-side expression contains parentheses or math.
                // If it doesn't, it's a plain column with an alias (e.g., "c.first_name alias")
                bool isExpr = expression.Contains('(') || expression.Contains(')') || expression.Contains('+');

                if (expression.EndsWith(" AS", StringComparison.OrdinalIgnoreCase))
                {
                    expression = expression.Substring(0, expression.Length - 3).TrimEnd();
                }

                string? tableAlias = null;
                string columnName = expression; //clean expression

                if (!isExpr && expression.Contains('.'))
                {
                    var parts = expression.Split('.');
                    tableAlias = parts[0];
                    columnName = parts[1];
                }

                var columnData = new ColumnMetadataModel()
                {
                    OriginalQuery = trimmed,
                    TableAlias = isExpr ? null : tableAlias,
                    ColumnName = columnName,
                    Alias = alias,
                    IsExpression = isExpr
                };

                _columns.Add(columnData);
            }
            else
            {
                // No space found at all, it's just a raw target column like "usr.user_id"
                string? tableAlias = null;
                string columnName = trimmed;

                if (trimmed.Contains('.'))
                {
                    var parts = trimmed.Split('.');
                    tableAlias = parts[0];
                    columnName = parts[1];
                }

                var columnData = new ColumnMetadataModel()
                {
                    OriginalQuery = trimmed,
                    TableAlias = tableAlias,
                    ColumnName = columnName,
                    Alias = null,
                    IsExpression = false
                };

                _columns.Add(columnData);
            }
        }
    }
}

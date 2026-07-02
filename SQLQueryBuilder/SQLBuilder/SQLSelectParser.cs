using SQLQueryBuilder.Model;
using System.Text;
using System.Text.RegularExpressions;

namespace SQLQueryBuilder.Builder
{
    internal class SQLSelectParser
    {
        private string _input = string.Empty;
        private int _index = 0;
        private List<ColumnMetadataModel> _columns = new();

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
                if (c == '\'' || c == '"')
                {
                    result.Append(ParseQuote());
                }
                else if (c == '(')
                {
                    result.Append(ParseParenthesis());
                }
                else if (CASECheck())
                {
                    result.Append(ParseCase());
                }
                else if (c == ',')
                {
                    Consume(); // Eat the comma

                    // SAFE GUARD: Only process if we actually accumulated something meaningful
                    string currentChunk = result.ToString().Trim();
                    if (!string.IsNullOrEmpty(currentChunk))
                    {
                        ProcessAndAdd(currentChunk);
                    }
                    result.Clear();
                }
                else
                {
                    result.Append(Consume());
                }
            }

            // SAFE GUARD: Handle the final column safely, ignoring trailing whitespace/commas
            string finalChunk = result.ToString().Trim();
            if (!string.IsNullOrEmpty(finalChunk))
            {
                ProcessAndAdd(finalChunk);
            }

            return _columns;
        }

        private string ParseQuote()
        {
            char quoteChar = Peek();
            if (quoteChar != '"' && quoteChar != '\'')
                throw new InvalidOperationException($"Expected a quote but got {Peek()} instead! at index:{_index}");

            var start = _index;
            var result = new StringBuilder();
            result.Append(Consume());

            while (!IsAtEnd() && Peek() != quoteChar)
            {
                result.Append(Consume());
            }

            if (IsAtEnd() || Peek() != quoteChar)
                throw new InvalidOperationException($"Missing closing quote! starting at {start}\n{_input.Substring(start)}");

            result.Append(Consume());
            return result.ToString();
        }

        private string ParseParenthesis()
        {
            if (Peek() != '(')
                throw new InvalidOperationException($"Expected a parenthesis but got {Peek()} instead! at index:{_index}");

            var start = _index;
            var result = new StringBuilder();
            var depth = 0;

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
                else if (c == ',')
                {
                    // SAFE GUARD: Inner function commas must NOT trigger a column split!
                    result.Append(Consume());
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
            if (!CASECheck())
                throw new InvalidOperationException($"Expected CASE but got {Peek()} instead! at index:{_index}");

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
                else if (c == ',')
                {
                    // SAFE GUARD: Commas inside a CASE statement belong to the expression!
                    result.Append(Consume());
                }
                else
                {
                    result.Append(Consume());
                }
            }

            if (IsAtEnd())
                throw new InvalidOperationException($"Missing closing CASE with END! starting from {start}\n{_input.Substring(start)}");

            result.Append(Consume()); // E
            result.Append(Consume()); // N
            result.Append(Consume()); // D

            return result.ToString();
        }

        private void Init(string input)
        {
            _input = input ?? string.Empty;
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

            // 1. EXTRACT THE ALIAS FIRST (Handles trailing quotes, brackets, or plain words)
            string expression = trimmed;
            string? alias = null;

            var i = trimmed.Length - 1;
            var lastChar = trimmed[trimmed.Length-1];
            var notLetter = !char.IsLetter(lastChar);
            //Getting the Index split for alias
            if (notLetter) // alias with bracket or quote
            {
                i--; // skip 1 character so its skip opening quote
                var targetChar = lastChar == ']' ? '[' : lastChar;
                while (i >= 0 && trimmed[i] != targetChar)
                {
                    i--;
                }
            }
            else // non quoted or bracketed alias
            {
                while (i >= 0 && trimmed[i] != ' ')
                {
                    i--;
                }
            }
            
            if(i != -1)
            {
                expression = trimmed.Substring(0, i).Trim();
                
                if (expression.EndsWith(" AS", StringComparison.InvariantCultureIgnoreCase))
                {
                    expression = expression.Substring(0, expression.Length - 3).Trim();
                }

                if (notLetter)
                {
                    alias = trimmed.Substring(i+1, trimmed.Length - i-2).Trim();
                }
                else
                {
                    alias = trimmed.Substring(i, trimmed.Length - i).Trim();
                }
            } 

            // 2. FLAG EXPRESSIONS UPFRONT
            // If the left side has spaces, keywords, or math operators, it's 100% an expression.
            bool isStringLiteral =
                expression.Length >= 2 &&
                expression.StartsWith('\'') &&
                expression.EndsWith('\'');

            bool isExpr = !IsSimpleColumnReference(expression);

            string? tableAlias = null;
            string columnName = expression;

            // 3. ROUTE LOGIC BASED ON THE FLAG
            if (!isExpr)
            {
                // Now it's perfectly safe to split on a dot because we KNOW it's a plain column!
                if (expression.Contains('.'))
                {
                    var parts = expression.Split('.');
                    tableAlias = parts[0];
                    columnName = parts[1];
                }
            }
            else
            {
                // For expressions, the "ColumnName" is just the entire formula/statement
                tableAlias = null;
                columnName = expression;
            }

            // 4. ADD TO METADATA
            _columns.Add(new ColumnMetadataModel()
            {
                OriginalQuery = trimmed,
                TableAlias = tableAlias,
                ColumnName = columnName,
                Alias = alias,
                IsExpression = isExpr
            });
        }
        private bool IsSimpleColumnReference(string expr)
        {
            expr = expr.Trim();

            // table.column
            if (Regex.IsMatch(expr, @"^[A-Za-z_]\w*\.[A-Za-z_]\w*$"))
                return true;

            // [table].[column]
            if (Regex.IsMatch(expr, @"^\[[^\]]+\]\.\[[^\]]+\]$"))
                return true;

            // column
            if (Regex.IsMatch(expr, @"^[A-Za-z_]\w*$"))
                return true;

            // [column]
            if (Regex.IsMatch(expr, @"^\[[^\]]+\]$"))
                return true;

            return false;
        }
    }
}
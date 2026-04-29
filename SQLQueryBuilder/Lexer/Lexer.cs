using SQLQueryBuilder.Model.ASTModel;

namespace SQLQueryBuilder.Lexer
{
    public class Lexer
    {
        List<Token> Tokens;
        public Lexer()
        {
            Tokens = new();
        }

        public List<Token> Tokenize(string input)
        {
            int i = 0;
            bool lastWasSymbol = false;

            while (i < input.Length)
            {
                char c = input[i];
                if (char.IsWhiteSpace(c))
                {
                    i++;
                    lastWasSymbol = false;
                    continue;
                }

                if (c == '(')
                {
                    i++;
                    Tokens.Add(new Token() { Type = TokenType.LParen, Value = c.ToString() });
                    lastWasSymbol = false;
                    continue;
                }

                if (c == ')')
                {
                    i++;
                    Tokens.Add(new Token() { Type = TokenType.RParen, Value = c.ToString() });
                    lastWasSymbol = false;
                    continue;
                }

                if ("=><!:".Contains(c))
                {
                    var start = i;
                    while (i < input.Length && "=><!:".Contains(input[i]))
                        i++;
                    string op = input.Substring(start, i - start);
                    Tokens.Add(new Token() { Type = TokenType.Operator, Value = op });
                    lastWasSymbol = true;
                    continue;
                }

                // Handle Digits specifically
                if (char.IsDigit(c))
                {
                    var start = i;
                    while (i < input.Length && char.IsDigit(input[i]))
                        i++;

                    string digit = input.Substring(start, i - start);
                    if (lastWasSymbol)
                    {
                        Tokens.Add(new Token() { Type = TokenType.Value, Value = digit });
                    }
                    else
                    {
                        Tokens.Add(new Token() { Type = TokenType.Identifier, Value = digit });
                    }

                    lastWasSymbol = false;
                    continue;
                }

                // Handle Letters specifically
                if (char.IsLetter(c))
                {
                    var start = i;
                    while (i < input.Length && char.IsLetter(input[i]))
                        i++;

                    string word = input.Substring(start, i - start);
                    if (word.Equals("and", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Tokens.Add(new Token() { Type = TokenType.And, Value = word });
                    }
                    else if (word.Equals("or", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Tokens.Add(new Token() { Type = TokenType.Or, Value = word });
                    }
                    else if (lastWasSymbol)
                    {
                        Tokens.Add(new Token() { Type = TokenType.Value, Value = word });
                    }
                    else
                    {
                        Tokens.Add(new Token() { Type = TokenType.Identifier, Value = word });
                    }
                    lastWasSymbol = false;
                    continue;
                }
                i++;
            }
            return Tokens;
        }
    }
}

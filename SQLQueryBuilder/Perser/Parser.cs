using SQLQueryBuilder.Model.ASTModel;

namespace SQLQueryBuilder.Perser
{
    public class Parser
    {
        private List<Token> _tokens;
        private int _current = 0;
        private string _input;

        public Parser(List<Token> tokens, string input)
        {
            _tokens = tokens;
            _input = input;
        }

        private Token Peek()
        {
            if (IsAtEnd()) return new Token() { Type = TokenType.EOF, Value = "EOF", Start = _input.Length, Length = 1 };
            return _tokens[_current];
        }

        private Token Consume()
        {
            if (IsAtEnd()) return new Token() { Type = TokenType.EOF, Value = "EOF", Start = _input.Length, Length = 1 };
            return _tokens[_current++];
        }

        private bool IsAtEnd() => _current >= _tokens.Count;

        // The starting point for parsing
        public Expr Parse()
        {
            var expression = ParseOr();

            if (!IsAtEnd())
            {
                var token = Peek();

                if (token.Type == TokenType.RParen)
                {
                    throw ParserErrorHelper.CreateError(_input, token,
                        "Unbalanced parenthesis. This closing ')' has no matching opening '(' earlier in the query.");
                }
                else if(token.Type != TokenType.EOF)
                {
                    throw ParserErrorHelper.CreateError(_input, token,
                    $"Unexpected '{token.Value}' found after the expression. Did you forget a logical operator (AND/OR)?");
                }
            }

            return expression;
        }

        private Expr ParseOr()
        {
            var left = ParseAnd();
            while (!IsAtEnd() && Peek().Type == TokenType.Or)
            {
                var op = Consume();

                var next = Peek();
                if (next.Type == TokenType.Operator || next.Type == TokenType.And || next.Type == TokenType.Or || next.Type == TokenType.RParen)
                {
                    throw ParserErrorHelper.CreateError(_input, next,
                        $"Logical operator '{op.Value}' cannot be followed by '{next.Value}'. " +
                        "Expected a field name or an opening parenthesis.");
                }
                var right = ParseAnd();
                left = new BinaryExpr()
                {
                    Left = left,
                    Op = op.Value,
                    Right = right
                };
            }
            return left;
        }
        private Expr ParseAnd()
        {
            var left = ParsePrimary();
            while (!IsAtEnd() && Peek().Type == TokenType.And)
            {
                var op = Consume();

                var next = Peek();
                if (next.Type == TokenType.Operator || next.Type == TokenType.And || next.Type == TokenType.Or || next.Type == TokenType.RParen)
                {
                    throw ParserErrorHelper.CreateError(_input, next,
                        $"Logical operator '{op.Value}' cannot be followed by '{next.Value}'. " +
                        "Expected a field name or an opening parenthesis.");
                }

                var right = ParsePrimary();
                left = new BinaryExpr()
                {
                    Left = left,
                    Op = op.Value,
                    Right = right
                };
            }
            return left;
        }
        private Expr ParsePrimary()
        {
            var token = Peek();

            if (token.Type == TokenType.LParen)
            {
                Consume(); // (

                if (Peek().Type == TokenType.RParen)
                {
                    throw ParserErrorHelper.CreateError(_input, Peek(),
                        "Empty expression. Parentheses must contain a search term (e.g., '(Name:Kingston)').");
                }
                var expr = ParseOr();
                if (Peek().Type != TokenType.RParen)
                {
                    // Pointing to the opening bracket that failed to close
                    throw ParserErrorHelper.CreateError(_input, token,
                        "This opening parenthesis is never closed.");
                }
                Consume(); // )
                return expr;
            }

            if (token.Type == TokenType.RParen)
            {
                throw ParserErrorHelper.CreateError(_input, token,
                    "Unexpected closing parenthesis. Did you forget an opening '('?");
            }

            return ParseComparison();
        }
        private Expr ParseComparison()
        {
            var left = ParseIdentifier();
            var token = Peek();

            if (!IsAtEnd() && token.Type == TokenType.Operator)
            {
                var right = ParseValue();
                var @operator = Consume();
                return new ComparisonExpr
                {
                    Identifier = left.Value,
                    Op = @operator.Value,
                    Value = right.Value
                };
            }
            else if(!IsAtEnd() && token.Type != TokenType.Operator)
            {
               throw ParserErrorHelper.CreateError(_input, token, $"Expected Operator, but found '{token.Value}'.");
            }

            return left;
        }
        private LiteralExpr ParseIdentifier()
        {
            var token = Peek();

            if (token.Type != TokenType.String &&
                token.Type != TokenType.Number)
            {
                throw ParserErrorHelper.CreateError(
                    _input,
                    token,
                    $"Expected Identifier, but found '{token.Value}'.");
            }

            return new LiteralExpr
            {
                Value = Consume().Value
            };
        }
        private LiteralExpr ParseValue()
        {
            var token = Peek();

            if (token.Type != TokenType.String &&
                token.Type != TokenType.Number)
            {
                throw ParserErrorHelper.CreateError(
                    _input,
                    token,
                    $"Expected Value, but found '{token.Value}'.");
            }

            if (token.Type == TokenType.Number)
            {
                if (!float.TryParse(token.Value, out _))
                {
                    throw ParserErrorHelper.CreateError(
                        _input,
                        token,
                        $"'{token.Value}' is not a valid number.");
                }
            }
            return new LiteralExpr
            {
                Value = Consume().Value
            };
        }
    }
}

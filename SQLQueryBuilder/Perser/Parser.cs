using SQLQueryBuilder.Model.ASTModel;

namespace SQLQueryBuilder.Perser
{
    internal class Parser
    {
        private List<Token> _tokens = new List<Token>();
        private int _current = 0;
        private string _input = "";
        //Utility
        private Token Peek()
        {
            if (IsAtEnd) return _tokens[_tokens.Count - 1];
            return _tokens[_current];
        }
        private Token Consume()
        {
            if (IsAtEnd)
                throw new Exception($"Consume past end at Index={_current}");
            return _tokens[_current++];
        }
        private bool IsAtEnd => _current >= _tokens.Count;
        private Token ExpectAny(
            string errorMessage,
            params TokenType[] validTypes)
        {
            var token = Peek();

            if (!validTypes.Contains(token.Type))
            {
                throw ParserErrorHelper.CreateError(
                    _input,
                    token,
                    errorMessage);
            }

            return Consume();
        }

        private bool Match(TokenType type)
        {
            if (Check(type))
            {
                Consume();
                return true;
            }

            return false;
        }
        private bool Check(TokenType type) => Peek().Type == type;

        // The starting point for parsing
        public Expr Parse(List<Token> tokens, string input)
        {
            _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _current = 0;

            var expression = ParseOr();

            if (!IsAtEnd)
            {
                if (Check(TokenType.RParen))
                {
                    throw ParserErrorHelper.CreateError(_input, Peek(),
                        "Unexpected closing parenthesis.");
                }
                if (!Check(TokenType.EOF))
                {
                    throw ParserErrorHelper.CreateError(_input, Peek(),
                    $"Unexpected '{Peek().Value}' found after the expression. Did you forget a logical operator (AND/OR)?");
                }
            }

            return expression;
        }

        private Expr ParseOr()
        {
            var left = ParseAnd();
            while (!IsAtEnd && Check(TokenType.OR))
            {
                var op = Consume();
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
            while (!IsAtEnd && Check(TokenType.AND))
            {
                var op = Consume();
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
            if (Match(TokenType.LParen))
            {
                if (Check(TokenType.RParen))
                {
                    throw ParserErrorHelper.CreateError(
                        _input,
                        Peek(),
                        "Empty expression.");
                }

                var expr = ParseOr();
                ExpectAny("Expected ')' after expression.",TokenType.RParen);
                return expr;
            }

            if (Check(TokenType.RParen))
            {
                throw ParserErrorHelper.CreateError(
                    _input,
                    Peek(),
                    "Unexpected closing parenthesis.");
            }

            return ParseComparison();
        }
        private Expr ParseComparison()
        {
            var left = ParseIdentifier();

            var @operator = ExpectAny(
                $"Expected operator, but found '{Peek().Value}'.",
                TokenType.Operator);

            var right = ParseValue();

            return new ComparisonExpr
            {
                Identifier = left.Value,
                Op = @operator.Value,
                Value = right.Value
            };
        }
        private LiteralExpr ParseIdentifier()
        {
            var token = ExpectAny($"Expected Identifier with type of {TokenType.String}, but found '{Peek().Value}'. with Type of '{Peek().Type}'.", TokenType.String);
            return new LiteralExpr
            {
                Value = token.Value
            };
        }
        private LiteralExpr ParseValue()
        {
            var token = ExpectAny($"Expected Value with type of {TokenType.String}/{TokenType.Number}, but found '{Peek().Value}'. with Type of '{Peek().Type}'."
                , TokenType.String, TokenType.Number);
            return new LiteralExpr
            {
                Value = token.Value
            };
        }
    }
}

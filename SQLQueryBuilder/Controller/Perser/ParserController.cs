using SQLQueryBuilder.Controller.Perser.Helper;
using SQLQueryBuilder.Enum;
using SQLQueryBuilder.Model.ASTModel;

namespace SQLQueryBuilder.Controller.Perser
{
    internal class ParserController
    {
        private List<TokenModel> _tokens = new List<TokenModel>();
        private int _current = 0;
        private string _input = "";
        //Utility
        private TokenModel Peek()
        {
            if (IsAtEnd) return _tokens[_tokens.Count - 1];
            return _tokens[_current];
        }
        private TokenModel Consume()
        {
            if (IsAtEnd)
                throw new Exception($"Consume past end at Index={_current}");
            return _tokens[_current++];
        }
        private bool IsAtEnd => _current >= _tokens.Count;
        private TokenModel ExpectAny(
            string errorMessage,
            params TokenTypeEnum[] validTypes)
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

        private bool Match(TokenTypeEnum type)
        {
            if (Check(type))
            {
                Consume();
                return true;
            }

            return false;
        }
        private bool Check(TokenTypeEnum type) => Peek().Type == type;

        // The starting point for parsing
        public ExpressionModel Parse(List<TokenModel> tokens, string input)
        {
            _tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _current = 0;

            var expression = ParseOr();

            if (!IsAtEnd)
            {
                if (Check(TokenTypeEnum.RParen))
                {
                    throw ParserErrorHelper.CreateError(_input, Peek(),
                        "Unexpected closing parenthesis.");
                }
                if (!Check(TokenTypeEnum.EOF))
                {
                    throw ParserErrorHelper.CreateError(_input, Peek(),
                    $"Unexpected '{Peek().Value}' found after the expression. Did you forget a logical operator (AND/OR)?");
                }
            }
            return expression;
        }

        private ExpressionModel ParseOr()
        {
            var left = ParseAnd();
            while (!IsAtEnd && Check(TokenTypeEnum.OR))
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
        private ExpressionModel ParseAnd()
        {
            var left = ParsePrimary();
            while (!IsAtEnd && Check(TokenTypeEnum.AND))
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
        private ExpressionModel ParsePrimary()
        {
            if (Match(TokenTypeEnum.LParen))
            {
                if (Check(TokenTypeEnum.RParen))
                {
                    throw ParserErrorHelper.CreateError(
                        _input,
                        Peek(),
                        "Empty expression.");
                }

                var expr = ParseOr();
                ExpectAny("Expected ')' after expression.",TokenTypeEnum.RParen);
                return expr;
            }

            if (Check(TokenTypeEnum.RParen))
            {
                throw ParserErrorHelper.CreateError(
                    _input,
                    Peek(),
                    "Unexpected closing parenthesis.");
            }

            return ParseComparison();
        }
        private ExpressionModel ParseComparison()
        {
            var left = ParseIdentifier();

            var @operator = ExpectAny(
                $"Expected operator, but found '{Peek().Value}'.",
                TokenTypeEnum.Operator);

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
            var token = ExpectAny($"Expected Identifier with type of {TokenTypeEnum.String}, but found '{Peek().Value}'. with Type of '{Peek().Type}'.", TokenTypeEnum.String);
            return new LiteralExpr
            {
                Value = token.Value
            };
        }
        private LiteralExpr ParseValue()
        {
            var token = ExpectAny($"Expected Value with type of {TokenTypeEnum.String}/{TokenTypeEnum.Number}, but found '{Peek().Value}'. with Type of '{Peek().Type}'."
                , TokenTypeEnum.String, TokenTypeEnum.Number);
            return new LiteralExpr
            {
                Value = token.Value
            };
        }
    }
}

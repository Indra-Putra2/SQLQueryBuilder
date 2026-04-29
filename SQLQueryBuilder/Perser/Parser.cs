using SQLQueryBuilder.Model.ASTModel;

namespace SQLQueryBuilder.Perser
{
    public class Parser
    {
        private List<Token> _tokens;
        private int _current = 0;

        public Parser(List<Token> tokens) => _tokens = tokens;

        private Token Peek()
        {
            if (IsAtEnd()) return null;
            return _tokens[_current];
        }

        private Token Advance()
        {
            if (!IsAtEnd()) _current++;
            return _tokens[_current - 1];
        }

        private bool IsAtEnd() => _current >= _tokens.Count;

        // The starting point for parsing
        public Expr Parse() => ParseOr();
        private Expr ParseOr()
        {
            var left = ParseAnd();

            while (!IsAtEnd() && Peek()?.Type == TokenType.Or)
            {
                Advance(); // consume OR
                var right = ParseAnd();

                left = new BinaryExpr()
                {
                    Left = left,
                    Op = "OR",
                    Right = right
                };
            }

            return left;
        }
        private Expr ParseAnd()
        {
            var left = ParseComparison();

            while (!IsAtEnd() && Peek()?.Type == TokenType.And)
            {
                Advance(); // consume AND
                var right = ParseComparison();

                left = new BinaryExpr()
                {
                    Left = left,
                    Op = "AND",
                    Right = right
                };
            }

            return left;
        }
        private Expr ParseComparison()
        {
            // 1. Get the left side (Brand, Price, etc.)
            var left = ParsePrimary();

            // 2. If the NEXT token is an operator (: < >), then this IS a comparison
            if (!IsAtEnd() && Peek()?.Type == TokenType.Operator)
            {
                var op = Advance();
                var right = ParsePrimary(); // Get the value (Kingston, 500)

                // Return a ComparisonExpr
                return new ComparisonExpr
                {
                    Identifier = (left as LiteralExpr)!.Value, // You'll need to cast or handle this
                    Op = op.Value,
                    Value = (right as LiteralExpr)!.Value
                };
            }

            // 3. If there's no operator, just pass the primary (like a bracketed group) up
            return left;
        }

        private Expr ParsePrimary()
        {
            var token = Peek();

            if (token.Type == TokenType.LParen)
            {
                Advance();
                var expr = ParseOr();
                if (Peek()?.Type == TokenType.RParen) Advance();
                return expr;
            }

            // This is the "Base Case" - just return a single piece of data
            return new LiteralExpr { Value = Advance().Value };
        }
    }
}

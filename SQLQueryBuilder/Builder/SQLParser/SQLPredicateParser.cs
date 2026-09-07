using SQLQueryBuilder.Helper;
using SQLQueryBuilder.Model.Context;
using SQLQueryBuilder.Model.SQL_Expression;
using SQLQueryBuilder.SQLEnum;
using System.Data;

namespace SQLQueryBuilder.Builder.SQLParser
{
    internal class SQLPredicateParser
    {
        private SQLParserHelper _helper;
        private SQLExpressionParser _expressionParser = new SQLExpressionParser();
        public SQLExpression Parse(SQLParserContext context)
        {
            _helper = new SQLParserHelper(context);

            var expr = _expressionParser.Parse(_helper);

            if (!IsPredicate(expr))
            {
                var err = ErrorFormatter.Create(
                _helper.Input,
                0,
                "Expression must be a predicate.",
                errorTextLength: _helper.Input.Length);

                throw new SyntaxErrorException(err);
            }

            return expr;
        }

        private static bool IsPredicate(SQLExpression expr)
        {
            return expr switch
            {
                ParenthesizedExpression parenthesized => IsPredicate(parenthesized.Expression),
                BinaryExpression binary
                    => IsPredicateBinary(binary),

                LikeExpression => true,
                BetweenExpression => true,
                InExpression => true,
                IsNullExpression => true,

                UnaryExpression unary
                    when unary.Operator == SQLUnaryOperator.NOT
                    => IsPredicate(unary.Operand),

                _ => false
            };
        }

        private static bool IsPredicateBinary(BinaryExpression binary)
        {
            return binary.Operator switch
            {
                SQLBinaryOperator.Equal
                or SQLBinaryOperator.NotEqual
                or SQLBinaryOperator.GreaterThan
                or SQLBinaryOperator.GreaterThanOrEqual
                or SQLBinaryOperator.LessThan
                or SQLBinaryOperator.LessThanOrEqual
                    => true,

                SQLBinaryOperator.AND
                or SQLBinaryOperator.OR
                    => IsPredicate(binary.Left) &&
                       IsPredicate(binary.Right),

                _ => false
            };
        }
    }
}

using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.SQL_Expression;
using SQLQueryBuilder.SQLEnum;
using System.Data;

namespace SQLQueryBuilder.Builder.SQLParser
{
    internal class SQLPredicateParser
    {
        private SQLParserHelper _helper;
        private SQLExpressionParser _expressionParser = new SQLExpressionParser();
        public SQLExpression Parse(string input, List<SQLToken> tokens)
        {
            _helper = new SQLParserHelper(input, tokens);

            var expr = _expressionParser.Parse(_helper);

            if (!IsPredicate(expr))
            {
                var err = _helper.CreateErrorMessage(
                _helper.Input,
                0,
                "JOIN condition must evaluate to a boolean.",
                errorTextLength: _helper.Input.Length);

                throw new SyntaxErrorException(err);
            }

            return expr;
        }

        private static bool IsPredicate(SQLExpression expr)
        {
            return expr switch
            {
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

using SQLQueryBuilder.Core.SQLEnum.SQLExpression;
using SQLQueryBuilder.Core.SQLEnum.Tokenizer;
using SQLQueryBuilder.Core.SQLExpression;
using SQLQueryBuilder.Helper;
using SQLQueryBuilder.Model.Context;
using System.Data;

namespace SQLQueryBuilder.Builder.SQLParser
{
    internal static class SQLPredicateParser
    {
        public static SQLExpression Parse(ref SQLParserContext context)
        {
            var expr = SQLExpressionParser.Parse(ref context);

            if (!IsPredicate(expr))
            {
                var err = ErrorFormatter.Create(
                context.Span.ToString(),
                0,
                "Expression must be a predicate.",
                errorTextLength: context.Span.ToString().Length);

                throw new SyntaxErrorException(err);
            }

            if (!SQLParserHelper.Expect(context.Current, SQLTokenType.EOF))
            {
                var err = ErrorFormatter.Create(
                    context.Span.ToString(),
                    context.Current.StartPosition,
                    "Unexpected token after expression.",
                    errorTextLength: context.Span.ToString().Length - context.Current.StartPosition);

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

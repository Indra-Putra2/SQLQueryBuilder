using SQLQueryBuilder.Core.SQLEnum.Tokenizer;
using SQLQueryBuilder.Core.SQLExpression;
using SQLQueryBuilder.Core.Tokenizer;
using SQLQueryBuilder.Helper;
using SQLQueryBuilder.Model.Context;
using System.Data;

namespace SQLQueryBuilder.Builder.SQLParser
{
    internal static class SQLOrderByParser
    {
        public static List<OrderByExpression> Parse(ref SQLParserContext context)
        {
            var orderBy = new List<OrderByExpression>();

            if (!context.IsAtEnd && SQLParserHelper.Expect(context.Current, SQLTokenType.Comma))
            {
                var err = ErrorFormatter.Create(
                    context.Span.ToString(),
                    context.Current.StartPosition,
                    $"Expected Expression but got '{context.GetSlice(context.Current)}'",
                    errorTextLength: context.Current.Length);
                throw new SyntaxErrorException(err);
            }
            var expr = SQLExpressionParser.Parse(ref context);
            var desc = false;
            SQLToken lastToken = context.Current;

            if (SQLParserHelper.IsCurrentKeyword(context, "DESC"))
            {
                context.Consume();
                desc = true;
            }
            else if (SQLParserHelper.IsCurrentKeyword(context, "ASC"))
            {
                context.Consume();
            }

            orderBy.Add(new OrderByExpression
            {
                Span = SQLParserHelper.CreateSpan(expr.Span.Start, lastToken.StartPosition, lastToken.Length),
                Expression = expr,
                Descending = desc
            });


            while (!context.IsAtEnd && SQLParserHelper.Expect(context.Current, SQLTokenType.Comma))
            {
                context.Consume(); // Consume ','

                expr = SQLExpressionParser.Parse(ref context);
                desc = false;
                lastToken = context.Current;

                if (SQLParserHelper.IsCurrentKeyword(context, "DESC"))
                {
                    context.Consume();
                    desc = true;
                }
                else if (SQLParserHelper.IsCurrentKeyword(context, "ASC"))
                {
                    context.Consume();
                }

                orderBy.Add(new OrderByExpression
                {
                    Span = SQLParserHelper.CreateSpan(expr.Span.Start, lastToken.StartPosition, lastToken.Length),
                    Expression = expr,
                    Descending = desc
                });
            }

            if (!SQLParserHelper.Expect(context.Current, SQLTokenType.EOF))
            {
                var err = ErrorFormatter.Create(
                    context.Span.ToString(),
                    context.Current.StartPosition,
                    $"Unexpected token '{context.GetSlice(context.Current)}' after end of expression.",
                    errorTextLength: context.Current.Length);

                throw new SyntaxErrorException(err);
            }

            return orderBy;
        }
    }
}

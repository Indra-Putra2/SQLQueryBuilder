using SQLQueryBuilder.Core.SQLEnum.Tokenizer;
using SQLQueryBuilder.Core.SQLExpression;
using SQLQueryBuilder.Helper;
using SQLQueryBuilder.Model.Context;
using System.Data;

namespace SQLQueryBuilder.Builder.SQLParser
{
    internal static class SQLGroupByParser
    {
        public static List<SQLExpression> Parse(ref SQLParserContext context)
        {
            var selected = new List<SQLExpression>();

            if (!context.IsAtEnd && SQLParserHelper.Expect(context.Current, SQLTokenType.Comma))
            {
                var err = ErrorFormatter.Create(
                    context.Span.ToString(),
                    context.Current.StartPosition,
                    $"Expected Expression but got '{context.GetSlice(context.Current)}'",
                    errorTextLength: context.Current.Length);
                throw new SyntaxErrorException(err);
            }

            selected.Add(SQLExpressionParser.Parse(ref context));

            while (!context.IsAtEnd && SQLParserHelper.Expect(context.Current, SQLTokenType.Comma))
            {
                context.Consume(); // Consume ','

                selected.Add(SQLExpressionParser.Parse(ref context));
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

            return selected;
        }
    }
}

using SQLQueryBuilder.Core.SQLEnum.Tokenizer;
using SQLQueryBuilder.Core.SQLExpression;
using SQLQueryBuilder.Helper;
using SQLQueryBuilder.Model.Context;
using System.Data;

namespace SQLQueryBuilder.Builder.SQLParser
{
    internal static class SQLFromParser
    {
        public static List<SQLIdentifier> Parse(ref SQLParserContext context)
        {
            var parts = new List<SQLIdentifier>();

            var columns = SQLExpressionParser.Parse(ref context);

            if (columns is not ColumnExpression column)
            {
                var err = ErrorFormatter.Create(
                    context.Span.ToString(),
                    0,
                    $"{context.Span} is not a valid table reference.",
                    errorTextLength: context.Span.Length);
                throw new SyntaxErrorException(err);
            }

            if (column.Table == null)
            {
                if (column.Column == null)
                {
                    var err = ErrorFormatter.Create(
                        context.Span.ToString(),
                        0,
                        $"Table reference cannot be empty.",
                        errorTextLength: context.Span.Length);
                    throw new SyntaxErrorException(err);
                }
            }
            else
            {
                parts.AddRange(column.Table);
            }

            parts.Add(column.Column);

            if (parts.Count > 4) // 4 part only
            {
                throw new SyntaxErrorException($"Invalid table name '{context.Span.ToString()}'. Maximum format is server.database.schema.table.");
            }

            if (!SQLParserHelper.Expect(context.Current, SQLTokenType.EOF))
            {
                var err = ErrorFormatter.Create(
                    context.Span.ToString(),
                    context.Current.StartPosition,
                    $"Unexpected token '{context.GetSlice(context.Consume())}' after table name. Aliases must be provided separately.",
                    errorTextLength: context.Current.Length);
                throw new SyntaxErrorException(err);
            }

            return parts;
        }
    }
}

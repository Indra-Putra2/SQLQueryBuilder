using SQLQueryBuilder.Core.SQLEnum.Tokenizer;
using SQLQueryBuilder.Core.SQLExpression;
using SQLQueryBuilder.Core.Tokenizer;
using SQLQueryBuilder.Helper;
using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.Context;
using System.Data;

namespace SQLQueryBuilder.Builder.SQLParser
{
    internal static class SQLSelectParser
    {
        public static List<SelectExpression> Parse(ref SQLParserContext context)
        {
            var selected = new List<SelectExpression>();

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
            var alias = ParseAlias(ref context);
            var lastStart = alias?.StartPosition ?? expr.Span.Start;
            var lastLength = alias?.Length ?? expr.Span.Length;

            selected.Add(new SelectExpression
            {
                Span = SQLParserHelper.CreateSpan(expr.Span.Start, lastStart, lastLength),
                Expression = expr,
                Alias = GetAliasName(alias, ref context)
            });

            while (!context.IsAtEnd && SQLParserHelper.Expect(context.Current, SQLTokenType.Comma))
            {
                context.Consume(); // Consume ','

                expr = SQLExpressionParser.Parse(ref context);
                alias = ParseAlias(ref context);
                lastStart = alias?.StartPosition ?? expr.Span.Start;
                lastLength = alias?.Length ?? expr.Span.Length;

                var select = new SelectExpression
                {
                    Span = SQLParserHelper.CreateSpan(expr.Span.Start, lastStart, lastLength),
                    Expression = expr,
                    Alias = GetAliasName(alias, ref context)
                };

                selected.Add(select);
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

        private static SQLToken? ParseAlias(ref SQLParserContext context)
        {
            if (!context.IsAtEnd && SQLParserHelper.Expects(context.Current, SQLDictionary.DelimitersAndTerminators)) return null;

            if (SQLParserHelper.IsCurrentKeyword(context, "AS"))
            {
                context.Consume(); // Consume AS
            }

            if (!SQLParserHelper.Expects(context.Current, SQLDictionary.AllowedAliases))
            {
                var err = ErrorFormatter.Create(
                    context.Span.ToString(),
                    context.Current.StartPosition,
                    $"'{context.GetSlice(context.Current)}' Is not a valid Alias",
                    errorTextLength: context.Current.Length);

                throw new SyntaxErrorException(err);
            }

            return context.Consume(); // Consume Alias
        }
        private static SQLIdentifier? GetAliasName(SQLToken? token, ref SQLParserContext context)
        {
            if (token == null) return null;
            return new SQLIdentifier
            {
                Value = SQLParserHelper.UnescapeIdentifier(context.GetSlice(token.Value)),
                Delimiter = SQLParserHelper.GetIdentifierDelimiter(token.Value)
            };
        }
    }
}
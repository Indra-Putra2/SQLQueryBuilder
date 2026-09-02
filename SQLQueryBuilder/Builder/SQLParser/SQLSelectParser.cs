using SQLQueryBuilder.Helper;
using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.Context;
using SQLQueryBuilder.Model.SQL_Expression;
using SQLQueryBuilder.SQLEnum;
using System.Data;

namespace SQLQueryBuilder.Builder.SQLParser
{
    internal class SQLSelectParser
    {
        private SQLParserHelper _helper;
        private SQLExpressionParser _expressionParser = new();
        public List<SelectExpression> Parse(SQLParserContext context)
        {
            _helper = new SQLParserHelper(context);
            var selected = new List<SelectExpression>();

            if (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.Comma))
            {
                var err = ErrorFormatter.Create(
                    _helper.Input,
                    _helper.Current.StartPosition,
                    $"Expected Expression but got '{_helper.GetSlice(_helper.Current)}'",
                    errorTextLength: _helper.Current.Length);
                throw new SyntaxErrorException(err);
            }

            var expr = _expressionParser.Parse(_helper);
            var alias = ParseAlias();
            var lastStart = alias?.StartPosition ?? expr.Span.Start;
            var lastLength = alias?.Length ?? expr.Span.Length;

            selected.Add(new SelectExpression 
            { 
                Span = CreateSpan(expr.Span.Start, lastStart, lastLength),
                Expression = expr, 
                Alias = GetAliasName(alias)
            });

            while (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.Comma))
            {
                _helper.Consume(); // Consume ','

                expr = _expressionParser.Parse(_helper);
                alias = ParseAlias();
                lastStart = alias?.StartPosition ?? expr.Span.Start;
                lastLength = alias?.Length ?? expr.Span.Length;

                var select = new SelectExpression
                {
                    Span = CreateSpan(expr.Span.Start, lastStart, lastLength),
                    Expression = expr,
                    Alias = GetAliasName(alias)
                };

                selected.Add(select);
            }

            if (!_helper.Expect(_helper.Current, SQLTokenType.EOF))
            {
                var err = ErrorFormatter.Create(
                    _helper.Input,
                    _helper.Current.StartPosition,
                    $"Unexpected token '{_helper.GetSlice(_helper.Current)}' after end of expression.",
                    errorTextLength: _helper.Current.Length);

                throw new SyntaxErrorException(err);
            }

            return selected;
        }

        private SQLToken? ParseAlias()
        {
            if (!_helper.IsAtEnd && _helper.Expects(_helper.Current, SQLDictionary.DelimitersAndTerminators)) return null;

            if (_helper.IsKeyword(_helper.Current, "AS"))
            {
                _helper.Consume(); // Consume AS
            }

            if (!_helper.Expects(_helper.Current, SQLDictionary.AllowedAliases))
            {
                var err = ErrorFormatter.Create(
                    _helper.Input,
                    _helper.Current.StartPosition,
                    $"'{_helper.GetSlice(_helper.Current)}' Is not a valid Alias",
                    errorTextLength: _helper.Current.Length);

                throw new SyntaxErrorException(err);
            }

            return _helper.Consume(); // Consume Alias
        }

        private SQLIdentifier? GetAliasName(SQLToken? token)
        {
            if (token == null) return null;
            return new SQLIdentifier
            {
                Value = SQLParserHelper.UnescapeIdentifier(_helper.GetSlice(token.Value)),
                Delimiter = SQLParserHelper.GetIdentifierDelimiter(token.Value)
            };
        }

        private SQLSourceSpan CreateSpan(int startPosition, int endStartPosition, int endLength)
        {
            int endPosition = endStartPosition + endLength;
            return new SQLSourceSpan
            {
                Start = startPosition,
                Length = endPosition - startPosition
            };
        }
    }
}
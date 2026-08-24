using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.SQL_Expression;
using SQLQueryBuilder.SQLEnum;
using System.Data;

namespace SQLQueryBuilder.Builder.SQLParser
{
    internal class SQLOrderByParser
    {
        private SQLParserHelper _helper;
        private SQLExpressionParser _expressionParser = new();
        public List<OrderByExpression> Parse(SQLQueryContext context)
        {
            _helper = new SQLParserHelper(context);
            var orderBy = new List<OrderByExpression>();

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
            var desc = false;
            SQLToken lastToken = _helper.Current;

            if (_helper.IsKeyword(_helper.Current, "DESC"))
            {
                _helper.Consume();
                desc = true;
            }
            else if (_helper.IsKeyword(_helper.Current, "ASC"))
            {
                _helper.Consume();
            }

            orderBy.Add(new OrderByExpression
            {
                Span = CreateSpan(expr.Span.Start, lastToken.StartPosition, lastToken.Length),
                Expression = expr,
                Descending = desc
            });


            while (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.Comma))
            {
                _helper.Consume(); // Consume ','

                expr = _expressionParser.Parse(_helper);
                desc = false;
                lastToken = _helper.Current;

                if (_helper.IsKeyword(_helper.Current, "DESC"))
                {
                    _helper.Consume();
                    desc = true;
                }
                else if (_helper.IsKeyword(_helper.Current, "ASC"))
                {
                    _helper.Consume();
                }

                orderBy.Add(new OrderByExpression
                {
                    Span = CreateSpan(expr.Span.Start, lastToken.StartPosition, lastToken.Length),
                    Expression = expr,
                    Descending = desc
                });
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

            return orderBy;
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

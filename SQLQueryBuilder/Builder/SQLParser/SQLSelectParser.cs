using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.SQL_Expression;
using SQLQueryBuilder.SQLEnum;
using System.Data;

namespace SQLQueryBuilder.Builder.SQLParser
{
    internal class SQLSelectParser
    {
        private SQLParserHelper _helper;
        private SQLExpressionParser _expressionParser = new();
        public List<SelectExpression> Parse(string input, List<SQLToken> tokens)
        {
            _helper = new SQLParserHelper(input, tokens);
            var selected = new List<SelectExpression>();

            if (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.Comma))
            {
                var err = _helper.CreateErrorMessage(
                    _helper.Input,
                    _helper.Current.StartPosition,
                    $"Expected Expression but got '{_helper.Current.Value}'",
                    errorTextLength: _helper.Current.RawLength);
                throw new SyntaxErrorException(err);
            }

            var expr = _expressionParser.Parse(_helper);
            var alias = ParseAlias();
            selected.Add(new SelectExpression { Expression = expr, Alias = alias });

            while (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.Comma))
            {
                _helper.Consume(); // Consume ','

                expr = _expressionParser.Parse(_helper);
                alias = ParseAlias();

                var select = new SelectExpression { Expression = expr, Alias = alias };

                selected.Add(select);
            }

            if (!_helper.Expect(_helper.Current, SQLTokenType.EOF))
            {
                var err = _helper.CreateErrorMessage(
                    _helper.Input,
                    _helper.Current.StartPosition,
                    $"Unexpected token '{_helper.Current.Value}' after end of expression.",
                    errorTextLength: _helper.Current.RawLength);

                throw new SyntaxErrorException(err);
            }

            return selected;
        }

        private string? ParseAlias()
        {
            if (!_helper.IsAtEnd && _helper.Expects(_helper.Current, SQLParserHelper.DelimitersAndTerminators)) return null;

            if (_helper.IsKeyword(_helper.Current, "AS"))
            {
                _helper.Consume(); // Consume AS
            }

            if (!_helper.Expects(_helper.Current, SQLParserHelper.AllowedAliases))
            {
                var err = _helper.CreateErrorMessage(
                    _helper.Input,
                    _helper.Current.StartPosition,
                    $"'{_helper.Current.Value}' Is not a valid Alias",
                    errorTextLength: _helper.Current.RawLength);

                throw new SyntaxErrorException(err);
            }

            return _helper.Consume().Value; // Consume Alias
        }
    }
}
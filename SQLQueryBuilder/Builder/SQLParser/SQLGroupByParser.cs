using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.SQL_Expression;
using SQLQueryBuilder.SQLEnum;
using System.Data;

namespace SQLQueryBuilder.Builder.SQLParser
{
    internal class SQLGroupByParser
    {
        private SQLParserHelper _helper;
        private SQLExpressionParser _expressionParser = new();
        public List<SQLExpression> Parse(string input, List<SQLToken> tokens)
        {
            _helper = new SQLParserHelper(input, tokens);
            var selected = new List<SQLExpression>();

            if (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.Comma))
            {
                var err = _helper.CreateErrorMessage(
                    _helper.Input,
                    _helper.Current.StartPosition,
                    $"Expected Expression but got '{_helper.Current.Value}'",
                    errorTextLength: _helper.Current.RawLength);
                throw new SyntaxErrorException(err);
            }

            selected.Add(_expressionParser.Parse(_helper));

            while (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.Comma))
            {
                _helper.Consume(); // Consume ','

                selected.Add(_expressionParser.Parse(_helper));
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
    }
}

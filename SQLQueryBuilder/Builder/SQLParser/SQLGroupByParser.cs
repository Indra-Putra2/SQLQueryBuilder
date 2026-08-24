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
        public List<SQLExpression> Parse(SQLQueryContext context)
        {
            _helper = new SQLParserHelper(context);
            var selected = new List<SQLExpression>();

            if (!_helper.IsAtEnd && _helper.Expect(_helper.Current, SQLTokenType.Comma))
            {
                var err = ErrorFormatter.Create(
                    _helper.Input,
                    _helper.Current.StartPosition,
                    $"Expected Expression but got '{_helper.GetSlice(_helper.Current)}'",
                    errorTextLength: _helper.Current.Length);
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
                var err = ErrorFormatter.Create(
                    _helper.Input,
                    _helper.Current.StartPosition,
                    $"Unexpected token '{_helper.GetSlice(_helper.Current)}' after end of expression.",
                    errorTextLength: _helper.Current.Length);

                throw new SyntaxErrorException(err);
            }

            return selected;
        }
    }
}

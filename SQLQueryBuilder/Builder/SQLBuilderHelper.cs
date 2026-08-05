using SQLQueryBuilder.Builder.SQLParser;
using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.SQL_Expression;
using SQLQueryBuilder.SQLEnum;
using System.Data;

namespace SQLQueryBuilder.Builder
{
    public class SQLBuilderHelper
    {
        private SQLTokenizer _tokenizer = new SQLTokenizer();
        private SQLExpressionParser _expressionParser = new SQLExpressionParser();
        private SQLSelectParser _selectParser = new SQLSelectParser();
        private SQLPredicateParser _predicateParser = new SQLPredicateParser();
        private SQLGroupByParser _groupByParser = new SQLGroupByParser();
        private SQLOrderByParser _orderByParser = new SQLOrderByParser();
        private SQLFromParser _fromParser = new SQLFromParser();
        private SQLParserHelper _helper;

        public List<SQLToken> BuildTokens(string input)
        {
            return _tokenizer.Tokenize(input);
        }

        public SQLExpression BuildExpression(string input, bool validateEnd = true)
        {
            _helper = new SQLParserHelper(input, BuildTokens(input));
            var expr = _expressionParser.Parse(_helper);

            if (validateEnd && !_helper.Expect(_helper.Current, SQLTokenType.EOF))
            {
                var err = _helper.CreateErrorMessage(
                    _helper.Input,
                    _helper.Current.StartPosition,
                    $"Unexpected token '{_helper.Current.Value}' after end of expression.",
                    errorTextLength: _helper.Current.RawLength);

                throw new SyntaxErrorException(err);
            }

            return expr;
        }

        public List<SelectExpression> BuildSelect(string input)
        {
            return _selectParser.Parse(input, BuildTokens(input));
        }

        public SQLExpression BuildPredicate(string input)
        {
            return _predicateParser.Parse(input, BuildTokens(input));
        }

        public List<SQLExpression> BuildGroupBy(string input)
        {
            return _groupByParser.Parse(input, BuildTokens(input));
        }

        public List<OrderByExpression> BuildOrderBy(string input)
        {
            return _orderByParser.Parse(input, BuildTokens(input));
        }

        public List<string> BuildFrom(string input)
        {
            return _fromParser.Parse(input, BuildTokens(input));
        }
    }
}

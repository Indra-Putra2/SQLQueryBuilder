using SQLQueryBuilder.Builder.SQLParser;
using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.SQL_Expression;

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

        public SQLQueryContext BuildTokens(string input)
        {
            return _tokenizer.Tokenize(input);
        }

        public SQLExpression BuildExpression(string input, bool validateEnd = false)
        {
            return _expressionParser.Parse(BuildTokens(input), validateEnd);
        }

        public List<SelectExpression> BuildSelect(string input)
        {
            return _selectParser.Parse(BuildTokens(input));
        }

        public SQLExpression BuildPredicate(string input)
        {
            return _predicateParser.Parse(BuildTokens(input));
        }

        public List<SQLExpression> BuildGroupBy(string input)
        {
            return _groupByParser.Parse(BuildTokens(input));
        }

        public List<OrderByExpression> BuildOrderBy(string input)
        {
            return _orderByParser.Parse(BuildTokens(input));
        }

        public List<string> BuildFrom(string input)
        {
            return _fromParser.Parse(BuildTokens(input));
        }
    }
}

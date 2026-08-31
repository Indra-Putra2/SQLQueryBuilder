using SQLQueryBuilder.Builder.SQLParser;
using SQLQueryBuilder.Model.Context;
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

        public SQLParserContext BuildTokens(string input)
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

        public string BuildAlias(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias))
                throw new ArgumentNullException("Alias cannot be null!");

            var context = BuildTokens(alias);

            if (context.Tokens.Count != 2)
            {
                throw new SyntaxErrorException(
                    $"'{alias}' is not a valid alias.");
            }

            var token = context.Tokens[0];

            bool validAlias =
                token.Type == SQLTokenType.Identifier ||
                token.Type == SQLTokenType.BracketIdentifier ||
                token.Type == SQLTokenType.DoubleQuoteIdentifier;

            if (!validAlias || context.Tokens[1].Type != SQLTokenType.EOF)
            {
                throw new SyntaxErrorException(
                    $"'{alias}' is not a valid alias.");
            }

            return context.GetSlice(context.Tokens[0]).ToString();
        }
    }
}

using SQLQueryBuilder.Builder;
using SQLQueryBuilder.Builder.SQLParser;
using SQLQueryBuilder.Interface;
using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.Context;
using SQLQueryBuilder.Model.SQL_Expression;
using SQLQueryBuilder.SQLEnum;
using System.Data;

namespace SQLQueryBuilder.Helper
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
        private SQLGenerator _generator = new SQLGenerator();

        public SQLParserContext BuildTokens(string input)
        {
            return _tokenizer.Tokenize(input);
        }

        public SQLExpression BuildExpression(string input, bool validateEnd = true)
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

        public List<SQLIdentifier> BuildFrom(string input)
        {
            return _fromParser.Parse(BuildTokens(input));
        }

        public SQLQueryResult BuildSQL(SQLQuery query)
        {
            _generator.BuildBaseQuery(query);
            _generator.BuildClauses(query);
            return _generator.Build();
        }

        public SQLIdentifier BuildAlias(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias))
                throw new ArgumentNullException("Alias cannot be null!");

            var context = BuildTokens(alias);

            if (context.Tokens.Count != 2)
            {
                string cause = context.Tokens.Any(t => t.Type == SQLTokenType.Dot)
                    ? "Aliases cannot contain table or schema prefixes (e.g., 'dbo.name')."
                    : $"Expected a single identifier, but found multiple tokens.";

                throw new SyntaxErrorException(
                    $"'{alias}' is not a valid alias. {cause}\n" +
                    $"Aliases must be a single name, such as name, [name], or \"name\".");
            }

            var token = context.Tokens[0];

            SQLIdentifierDelimiter delimiter = SQLParserHelper.GetIdentifierDelimiter(token);
            var name = SQLParserHelper.UnescapeIdentifier(context.GetSlice(context.Tokens[0]));

            bool validAlias =
                token.Type == SQLTokenType.Identifier ||
                token.Type == SQLTokenType.BracketIdentifier ||
                token.Type == SQLTokenType.DoubleQuoteIdentifier;

            if (!validAlias || context.Tokens[1].Type != SQLTokenType.EOF)
            {
                throw new SyntaxErrorException(
                    $"'{alias}' is not a valid alias." +
                    $"\nAlias must be of type Identifier, Bracket identifier or Double quote identifier " +
                    $"\ne.g., name, [name], \"name\"");
            }
            var aliasIden = new SQLIdentifier
            {
                Value = name,
                Delimiter = delimiter
            };
            return aliasIden;
        }

        public IASTTraverser ASTWalkerBuiler(IASTVisitor walker) => new ASTWalker(walker);
    }
}

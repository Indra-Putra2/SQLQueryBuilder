using Dapper;
using SQLQueryBuilder.Builder;
using SQLQueryBuilder.Builder.SQLParser;
using SQLQueryBuilder.Interface;
using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.Context;
using SQLQueryBuilder.Model.QueryBuilder;
using SQLQueryBuilder.Model.SQL_Expression;
using SQLQueryBuilder.SQLEnum.SQLExpression;
using SQLQueryBuilder.SQLEnum.Tokenizer;
using System.Data;
using System.Text;

namespace SQLQueryBuilder.Helper
{
    public static class SQLBuilderHelper
    {
        // Tokenizer
        public static SQLParserContext BuildTokens(string input)
        {
            return SQLTokenizer.Tokenize(input);
        }

        // Parser
        public static SQLExpression BuildExpression(string input, bool validateEnd = true)
        {
            var context = BuildTokens(input);
            return SQLExpressionParser.Parse(ref context, validateEnd);
        }

        public static List<SelectExpression> BuildSelect(string input)
        {
            var context = BuildTokens(input);
            return SQLSelectParser.Parse(ref context);
        }

        public static SQLExpression BuildPredicate(string input)
        {
            var context = BuildTokens(input);
            return SQLPredicateParser.Parse(ref context);
        }

        public static List<SQLExpression> BuildGroupBy(string input)
        {
            var context = BuildTokens(input);
            return SQLGroupByParser.Parse(ref context);
        }

        public static List<OrderByExpression> BuildOrderBy(string input)
        {
            var context = BuildTokens(input);
            return SQLOrderByParser.Parse(ref context);
        }

        public static List<SQLIdentifier> BuildFrom(string input)
        {
            var context = BuildTokens(input);
            return SQLFromParser.Parse(ref context);
        }

        public static SQLQueryResult BuildSQL(SQLQuery query)
        {
            var context = new SQLGeneratorContext(new StringBuilder(), new DynamicParameters());
            
            SQLGenerator.BuildSelect(query.Select, ref context);
            SQLGenerator.BuildFrom(query.From, ref context);
            SQLGenerator.BuildJoins(query.Joins, ref context);

            SQLGenerator.BuildHaving(query.Having, ref context);
            SQLGenerator.BuildWhere(query.Where, ref context);
            SQLGenerator.BuildGroupBy(query.GroupBy, ref context);
            SQLGenerator.BuildOrderBy(query.OrderBy, ref context);
            SQLGenerator.BuildLimit(query.Limit, ref context);

            return SQLGenerator.Build(ref context);
        }

        public static SQLIdentifier BuildAlias(string alias)
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

        public static IASTTraverser ASTWalkerBuiler(IASTVisitor walker) => new ASTWalker(walker);
    }
}

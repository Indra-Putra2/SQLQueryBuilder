using Dapper;
using SQLQueryBuilder.Builder;
using SQLQueryBuilder.Builder.SQLParser;
using SQLQueryBuilder.Core.SQLEnum.Tokenizer;
using SQLQueryBuilder.Core.SQLExpression;
using SQLQueryBuilder.Interface;
using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.Context;
using SQLQueryBuilder.Model.QueryBuilder;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;
using System.Text;

namespace SQLQueryBuilder.Helper
{
    internal static class SQLBuilderHelper
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

        public static SQLIdentifier BuildAlias(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias))
                throw new ArgumentNullException("Table alias cannot be null or empty!");

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

            var expr = BuildExpression(alias);
            if (expr is not ColumnExpression column)
            {
                throw new SyntaxErrorException(
                    $"'{alias}' is not a valid alias." +
                    $"\nAlias must be of type Identifier, Bracket identifier or Double quote identifier " +
                    $"\ne.g., name, [name], \"name\"");
            }

            return column.Column;
        }

        // Source Maker
        public static (TableSource source, string rawName) BuildTableSource(string alias, Type type)
        {
            var rawName = type.GetCustomAttribute<TableAttribute>()?.Name ?? type.Name;
            var name = SQLBuilderHelper.BuildFrom(rawName);

            var source = new TableSource
            {
                Type = type,
                TableName = name,
                Alias = SQLBuilderHelper.BuildAlias(alias)
            };

            return (source, rawName);
        }

        public static JoinSource BuildJoinSource(JoinInfo joinInfo)
        {
            var table = BuildTableSource(joinInfo.TableAlias, joinInfo.TableType);
            return new JoinSource
            {
                Table = table.source,
                Condition = BuildPredicate(joinInfo.JoinCondition),
                JoinType = joinInfo.JoinType
            };
        }
        public static JoinSource BuildJoinSource(TableSource tableSource, JoinInfo joinInfo)
        {
            return new JoinSource
            {
                Table = tableSource,
                Condition = BuildPredicate(joinInfo.JoinCondition),
                JoinType = joinInfo.JoinType
            };
        }
        // AST walker
        public static IASTTraverser ASTWalkerBuiler(IASTVisitor walker) => new ASTWalker(walker);
    }
}

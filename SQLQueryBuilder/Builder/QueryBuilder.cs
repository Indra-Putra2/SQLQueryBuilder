using SQLQueryBuilder.Builder.SQLParser;
using SQLQueryBuilder.Interface;
using SQLQueryBuilder.Model;
using SQLQueryBuilder.SQLEnum;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;

namespace SQLQueryBuilder.Builder
{
    public class QueryBuilder : IQueryFrom, IQuerySelect, IQuery, IQueryFilter
    {
        private SQLQuery _query = new();
        private SQLBuilderHelper _builderHelper;
        private SQLSemanticValidator _validator;

        private QueryBuilder(SQLValidationMode mode)
        {
            _builderHelper = new SQLBuilderHelper();
            _validator = new SQLSemanticValidator(mode);
        }

        public static IQueryFrom Create(SQLValidationMode mode) => new QueryBuilder(mode);

        public IQuerySelect From<T>(string tableAlias)
        {
            var name = _builderHelper.BuildFrom(GetTableName<T>());
            _query.From = new TableSource { Type = typeof(T), TableName = name, Alias = ParseAlias(tableAlias) };
            return this;
        }

        public IQuery Select(string colums)
        {
            _query.Select = _builderHelper.BuildSelect(colums);
            return this;
        }
        public IQuery Join<T>(string tableAlias, string joinCondition, SQLJoinType joinType = SQLJoinType.INNER)
        {
            var name = _builderHelper.BuildFrom(GetTableName<T>());
            var table = new TableSource { Type = typeof(T), TableName = name, Alias = ParseAlias(tableAlias) };
            var join = new JoinSource { Table = table, Condition = _builderHelper.BuildPredicate(joinCondition), JoinType = joinType };
            _query.Joins.Add(join);
            return this;
        }
        public IQueryFilter GroupBy(string groupBy)
        {
            _query.GroupBy = _builderHelper.BuildGroupBy(groupBy);
            return this;
        }

        public IQueryFilter OrderBy(string orderBy)
        {
            _query.OrderBy = _builderHelper.BuildOrderBy(orderBy); ;
            return this;
        }

        public IQueryFilter Where(string where)
        {
            _query.Where = _builderHelper.BuildExpression(where);
            return this;
        }

        public IQueryFilter Having(string having)
        {
            _query.Having = _builderHelper.BuildExpression(having);
            return this;
        }

        public IQueryFilter Validate()
        {
            _validator.ValidateBaseQuery(_query);
            return this;
        }

        public SQLQueryData Build()
        {
            return null;
        }

        private string GetTableName<T>()
        {
            var type = typeof(T);
            return type.GetCustomAttribute<TableAttribute>()?.Name ?? type.Name;
        }

        private string ParseAlias(string? alias)
        {
            if (string.IsNullOrWhiteSpace(alias))
                return null;

            var tokens = _builderHelper.BuildTokens(alias);

            if (tokens.Count != 2)
            {
                throw new SyntaxErrorException(
                    $"'{alias}' is not a valid alias.");
            }

            var token = tokens[0];

            bool validAlias =
                token.Type == SQLTokenType.Identifier ||
                token.Type == SQLTokenType.BracketIdentifier ||
                token.Type == SQLTokenType.DoubleQuoteIdentifier;

            if (!validAlias || tokens[1].Type != SQLTokenType.EOF)
            {
                throw new SyntaxErrorException(
                    $"'{alias}' is not a valid alias.");
            }

            return token.Value;
        }
    }
}
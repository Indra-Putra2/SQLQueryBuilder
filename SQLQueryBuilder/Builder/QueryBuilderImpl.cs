using SQLQueryBuilder.Builder.SQLParser;
using SQLQueryBuilder.Helper;
using SQLQueryBuilder.Interface;
using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.SQL_Expression;
using SQLQueryBuilder.SQLEnum;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace SQLQueryBuilder.Builder
{
    internal class QueryBuilderImpl<TBase> : IQueryFrom, IQuerySelect, IQuery, IQueryFilter
    {
        private bool _baseValidated;
        private SQLQuery _query;
        private SQLBuilderHelper _builderHelper;
        private SQLSemanticValidator _validator;
        private SQLGenerator _queryGenerator;

        internal QueryBuilderImpl()
        {
            _baseValidated = false;
            _queryGenerator = new SQLGenerator();
            _builderHelper = new SQLBuilderHelper();
            _validator = new SQLSemanticValidator();
            _query = new SQLQuery
            {
                OutputDTO = typeof(TBase)
            };
        }
        private QueryBuilderImpl(
            SQLQuery query,
            SQLBuilderHelper builderHelper,
            SQLSemanticValidator validator)
        {
            _query = query;
            _builderHelper = builderHelper;
            _validator = validator;
            _queryGenerator = new SQLGenerator();

            _baseValidated = true;
        }
        public IQuerySelect From<T>(string tableAlias)
        {
            var rawName = GetTableName<T>();
            var name = _builderHelper.BuildFrom(rawName);

            var table = new TableSource
            {
                Type = typeof(T),
                TableName = name,
                Alias = _builderHelper.BuildAlias(tableAlias)
            };

            _query.From = new SQLSource<TableSource>
            {
                OriginalText = rawName,
                Value = table
            };

            return this;
        }

        public IQuery Select(string colums)
        {
            _query.Select = new SQLSource<List<SelectExpression>>
            {
                OriginalText = colums,
                Value = _builderHelper.BuildSelect(colums)
            };
            return this;
        }
        public IQuery Join<T>(string tableAlias, string joinCondition, SQLJoinType joinType = SQLJoinType.INNER)
        {
            var name = _builderHelper.BuildFrom(GetTableName<T>());

            var table = new TableSource
            {
                Type = typeof(T),
                TableName = name,
                Alias = _builderHelper.BuildAlias(tableAlias)
            };

            var join = new JoinSource
            {
                Table = table,
                Condition = _builderHelper.BuildPredicate(joinCondition),
                JoinType = joinType
            };

            _query.Joins.Add(new SQLSource<JoinSource> { OriginalText = joinCondition, Value = join });
            return this;
        }

        public IQueryFilter Where(string? where)
        {
            _query.Where = null;

            if (!string.IsNullOrWhiteSpace(where))
                _query.Where = new SQLSource<SQLExpression>
                {
                    OriginalText = where,
                    Value = _builderHelper.BuildPredicate(where)
                };

            return this;
        }

        public IQueryFilter GroupBy(string? groupBy)
        {
            _query.GroupBy = null;

            if (!string.IsNullOrWhiteSpace(groupBy))
                _query.GroupBy = new SQLSource<List<SQLExpression>>
                {
                    OriginalText = groupBy,
                    Value = _builderHelper.BuildGroupBy(groupBy)
                };

            return this;
        }

        public IQueryFilter Having(string? having)
        {
            _query.Having = null;

            if (!string.IsNullOrWhiteSpace(having))
                _query.Having = new SQLSource<SQLExpression>
                {
                    OriginalText = having,
                    Value = _builderHelper.BuildPredicate(having)
                };

            return this;
        }

        public IQueryFilter OrderBy(string? orderBy)
        {
            _query.OrderBy = null;

            if (!string.IsNullOrWhiteSpace(orderBy))

                _query.OrderBy = new SQLSource<List<OrderByExpression>>
                {
                    OriginalText = orderBy,
                    Value = _builderHelper.BuildOrderBy(orderBy)
                };

            return this;
        }

        public IQueryFilter Limit(int? limit)
        {
            _query.Limit = null;
            if(limit != null && limit >= 0)
                _query.Limit = limit;
            return this;
        }

        public IQueryFilter ValidateBaseQuery()
        {
            _validator.ValidateBaseQuery(_query);
            _queryGenerator.BuildBaseQuery(_query);
            _baseValidated = true;
            return this;
        }

        public IQueryFilter CreateQuery()
        {
            return new QueryBuilderImpl<TBase>(
                _query.CreateBaseCopy(),
                new SQLBuilderHelper(),
                new SQLSemanticValidator());
        }

        public SQLQueryResult Build()
        {
            if (!_baseValidated)
            {
                _validator.ValidateBaseQuery(_query);
                _queryGenerator.BuildBaseQuery(_query);
                _baseValidated = true;
            }

            _validator.ValidateClauses(_query);
            _queryGenerator.BuildClauses(_query);

            return _queryGenerator.Build();
        }

        private string GetTableName<T>()
        {
            var type = typeof(T);
            return type.GetCustomAttribute<TableAttribute>()?.Name ?? type.Name;
        }
    }
}
using Dapper;
using SQLQueryBuilder.Builder.SQLParser;
using SQLQueryBuilder.Core.SQLExpression;
using SQLQueryBuilder.Helper;
using SQLQueryBuilder.Interface;
using SQLQueryBuilder.Model.Context;
using SQLQueryBuilder.Model.QueryBuilder;
using SQLQueryBuilder.SQLEnum.QueryBuilder;
using System.Text;

namespace SQLQueryBuilder.Builder
{
    internal class QueryBuilderImpl<TBase> : IQueryFrom, IQuerySelect, IQuery, IQueryFilter
    {
        //new
        private SQLQueryRaw _sQLQuery;
        private SQLGeneratorContext _GeneratorContext;
        private SQLTableManager _tableManager;
        private SQLDTOManager _dtoManager;

        internal QueryBuilderImpl()
        {
            _GeneratorContext = new SQLGeneratorContext(
                new StringBuilder(),
                new DynamicParameters());

            _sQLQuery = new SQLQueryRaw
            {
                OutputDTO = typeof(TBase)
            };

            _tableManager = new SQLTableManager();
            _dtoManager = new SQLDTOManager();
        }
        private QueryBuilderImpl(
            SQLQueryRaw sQLQuery,
            SQLTableManager tableManager,
            SQLDTOManager dtoManager,
            SQLGeneratorContext baseContext)
        {
            _tableManager = tableManager;
            _dtoManager = dtoManager;

            // context Setup
            _sQLQuery = sQLQuery.ClearFilter();

            var sb = new StringBuilder().Append(baseContext.TemporaryString);
            var param = new DynamicParameters();
            param.AddDynamicParams(baseContext.TemporaryParameters);

            _GeneratorContext = baseContext with
            {
                TemporaryString = sb,
                TemporaryParameters = param,
            };
        }
        public IQuerySelect From<T>(string tableAlias)
        {
            _sQLQuery.From = (tableAlias, typeof(T));
            return this;
        }
        public IQuery Select(string colums)
        {
            _sQLQuery.Select = colums;
            return this;
        }
        public IQuery Join<T>(string tableAlias, string joinCondition, SQLJoinType joinType = SQLJoinType.INNER)
        {
            var joinInfo = new JoinInfo
            {
                TableAlias = tableAlias,
                JoinCondition = joinCondition,
                TableType = typeof(T),
                JoinType = joinType
            };
            _sQLQuery.Joins.Add(joinInfo);
            return this;
        }
        public IQueryFilter Where(string? where)
        {
            _sQLQuery.Where = where;
            return this;
        }
        public IQueryFilter GroupBy(string? groupBy)
        {
            _sQLQuery.GroupBy = groupBy;
            return this;
        }
        public IQueryFilter Having(string? having)
        {
            _sQLQuery.Having = having;
            return this;
        }
        public IQueryFilter OrderBy(string? orderBy)
        {
            _sQLQuery.OrderBy = orderBy;
            return this;
        }
        public IQueryFilter Limit(int? limit)
        {
            _sQLQuery.Limit = limit;
            return this;
        }

        public IQueryFilter ValidateBaseQuery()
        {
            if (_GeneratorContext.BaseValidated)
                return this;

            var validatorContext = new SQLValidatorContext
            {
                DtoManager = _dtoManager,
                TableManager = _tableManager,
            };

            // Registration
            validatorContext.DtoManager.RegisterDTO(_sQLQuery.OutputDTO);

            // From
            var fromTable = SQLBuilderHelper.BuildTableSource(_sQLQuery.From.alias, _sQLQuery.From.type);
            validatorContext.Span = fromTable.rawName.AsSpan();
            SQLSemanticValidator.ValidateFrom(fromTable.source, ref validatorContext);

            // Joins
            List<JoinSource>? joins = null;
            if (_sQLQuery.Joins.Count > 0)
            {
                joins = new List<JoinSource>();
                foreach (var join in _sQLQuery.Joins)
                {
                    var joinSource = SQLBuilderHelper.BuildJoinSource(join);
                    joins.Add(joinSource);

                    validatorContext.Span = join.JoinCondition.AsSpan();
                    SQLSemanticValidator.ValidateJoins(joinSource, ref validatorContext);
                }
            }

            // Select
            var selects = SQLBuilderHelper.BuildSelect(_sQLQuery.Select);
            validatorContext.Span = _sQLQuery.Select.AsSpan();
            SQLSemanticValidator.ValidateSelect(selects, ref validatorContext);

            // Generate base
            SQLGenerator.BuildSelect(selects, ref _GeneratorContext);
            SQLGenerator.BuildFrom(fromTable.source, ref _GeneratorContext);
            if (joins != null)
                SQLGenerator.BuildJoins(joins, ref _GeneratorContext);

            _GeneratorContext.SetBase();
            return this;
        }

        public IQueryFilter CopyBase()
        {
            ValidateBaseQuery();

            return new QueryBuilderImpl<TBase>(
                _sQLQuery,
                _tableManager,
                _dtoManager,
                _GeneratorContext);
        }

        public SQLQueryResult Build()
        {
            ValidateBaseQuery();

            _GeneratorContext.RefreshToBase();

            var where = _sQLQuery.Where;
            var having = _sQLQuery.Having;
            var orderBy = _sQLQuery.OrderBy;
            var groupBy = _sQLQuery.GroupBy;

            var whereNullOrEmpty = string.IsNullOrWhiteSpace(where);
            var havingNullOrEmpty = string.IsNullOrWhiteSpace(having);
            var orderByNullOrEmpty = string.IsNullOrWhiteSpace(orderBy);
            var groupByNullOrEmpty = string.IsNullOrWhiteSpace(groupBy);

            if (!whereNullOrEmpty ||
                !havingNullOrEmpty ||
                !orderByNullOrEmpty ||
                !groupByNullOrEmpty)
            {
                var context = new SQLValidatorContext
                {
                    DtoManager = _dtoManager,
                    TableManager = _tableManager,
                };

                if (!whereNullOrEmpty)
                {
                    context.Span = where.AsSpan();
                    var whereExpr = SQLBuilderHelper.BuildExpression(where);
                    SQLSemanticValidator.ValidateWhere(whereExpr, ref context);
                    SQLGenerator.BuildWhere(whereExpr, ref _GeneratorContext);
                }

                List<SQLExpression>? groupByExpr = null;
                if (!groupByNullOrEmpty)
                {
                    context.Span = groupBy.AsSpan();
                    groupByExpr = SQLBuilderHelper.BuildGroupBy(groupBy);

                    SQLSemanticValidator.ValidateGroupBy(groupByExpr, ref context);
                    SQLGenerator.BuildGroupBy(groupByExpr, ref _GeneratorContext);
                }

                if (!havingNullOrEmpty)
                {
                    context.Span = having.AsSpan();
                    var havingExpr = SQLBuilderHelper.BuildExpression(having);
                    SQLSemanticValidator.ValidateHaving(havingExpr, groupByExpr, ref context);
                    SQLGenerator.BuildHaving(havingExpr, ref _GeneratorContext);
                }

                if (!orderByNullOrEmpty)
                {
                    context.Span = orderBy.AsSpan();
                    var orderByExpr = SQLBuilderHelper.BuildOrderBy(orderBy);
                    SQLSemanticValidator.ValidateOrderBy(orderByExpr, ref context);
                    SQLGenerator.BuildOrderBy(orderByExpr, ref _GeneratorContext);
                }
            }

            SQLGenerator.BuildLimit(_sQLQuery.Limit, ref _GeneratorContext);

            return SQLGenerator.Build(ref _GeneratorContext);
        }
    }
}
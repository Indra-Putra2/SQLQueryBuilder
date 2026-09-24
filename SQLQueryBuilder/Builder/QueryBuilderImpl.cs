using SQLQueryBuilder.Builder.SQLParser;
using SQLQueryBuilder.Helper;
using SQLQueryBuilder.Interface;
using SQLQueryBuilder.Model.Context;
using SQLQueryBuilder.Model.QueryBuilder;
using SQLQueryBuilder.Model.SQL_Expression;
using SQLQueryBuilder.SQLEnum.QueryBuilder;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text;

namespace SQLQueryBuilder.Builder
{
    internal class QueryBuilderImpl<TBase> : IQueryFrom, IQuerySelect, IQuery, IQueryFilter
    {
        //new
        private SQLQueryNew _sQLQuery = null;
        private StringBuilder _sb = null;
        private SQLTableManager _tableManager = null;
        private SQLDTOManager _dtoManager = null;
        private bool _baseValidated;

        //old
        private SQLGenerator _queryGenerator;

        internal QueryBuilderImpl()
        {
            _baseValidated = false;
            _queryGenerator = new SQLGenerator();

            _sQLQuery = new SQLQueryNew
            {
                OutputDTO = typeof(TBase)
            };

            _tableManager = new SQLTableManager();
            _dtoManager = new SQLDTOManager();
        }
        private QueryBuilderImpl(
            SQLQueryNew sQLQuery,
            SQLTableManager tableManager,
            SQLDTOManager dtoManager)
        {
            _sQLQuery = sQLQuery;

            _queryGenerator = new SQLGenerator();

            _baseValidated = true;
            _tableManager = tableManager;
            _dtoManager = dtoManager;
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
                Type = typeof(T),
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
            if (_baseValidated)
                return this;

            var context = new SemanticValidatorContext
            {
                DtoManager = _dtoManager,
                TableManager = _tableManager,
            };

            // Registration
            context.DtoManager.RegisterDTO(_sQLQuery.OutputDTO);

            // From
            var table = CreateTableSource(_sQLQuery.From.alias, _sQLQuery.From.type);
            context.Span = table.rawName.AsSpan();
            SQLSemanticValidator.ValidateFrom(table.source, context);

            // Joins
            foreach (var join in _sQLQuery.Joins)
            {
                table = CreateTableSource(join.TableAlias, join.Type);

                var joinSource = new JoinSource
                {
                    Table = table.source,
                    Condition = SQLBuilderHelper.BuildPredicate(join.JoinCondition),
                    JoinType = join.JoinType
                };
                context.Span = join.JoinCondition.AsSpan();
                SQLSemanticValidator.ValidateJoins(joinSource, context);
            }

            // Select
            context.Span = _sQLQuery.Select.AsSpan();
            SQLSemanticValidator.ValidateSelect(SQLBuilderHelper.BuildSelect(_sQLQuery.Select), context);

            _baseValidated = true;
            return this;
        }

        public IQueryFilter Copy()
        {
            return new QueryBuilderImpl<TBase>(
                _sQLQuery.ClearFilter(),
                _tableManager,
                _dtoManager);
        }

        public SQLQueryResult Build()
        {
            if (!_baseValidated)
            {
                ValidateBaseQuery();
            }

            var where = _sQLQuery.Where;
            var having = _sQLQuery.Having;
            var orderBy = _sQLQuery.OrderBy;
            var groupBy = _sQLQuery.GroupBy;

            if (where != null || having != null || orderBy != null || groupBy != null)
            {
                var context = new SemanticValidatorContext
                {
                    DtoManager = _dtoManager,
                    TableManager = _tableManager,
                };

                if (where != null)
                {
                    context.Span = where.AsSpan();
                    SQLSemanticValidator.ValidateWhere(SQLBuilderHelper.BuildExpression(where), context);
                }

                List<SQLExpression>? groupByExpr = null;
                if (groupBy != null)
                {
                    context.Span = groupBy.AsSpan();
                    groupByExpr = SQLBuilderHelper.BuildGroupBy(groupBy);
                    SQLSemanticValidator.ValidateGroupBy(groupByExpr, context);
                }

                if (having != null)
                {
                    if (groupByExpr == null)
                    {
                        throw new InvalidOperationException("A HAVING clause cannot be used without a GROUP BY clause.");
                    }
                    context.Span = having.AsSpan();
                    var havingExpr = SQLBuilderHelper.BuildExpression(having);
                    SQLSemanticValidator.ValidateHaving(havingExpr, groupByExpr, context);
                }

                if (orderBy != null)
                {
                    context.Span = orderBy.AsSpan();
                    SQLSemanticValidator.ValidateOrderBy(SQLBuilderHelper.BuildOrderBy(orderBy), context);
                }
            }

            return null;
        }

        private string GetTableName(Type type) => type.GetCustomAttribute<TableAttribute>()?.Name ?? type.Name;
        private (TableSource source, string rawName) CreateTableSource(string alias, Type type)
        {
            var rawName = GetTableName(type);
            var name = SQLBuilderHelper.BuildFrom(rawName);

            var source = new TableSource
            {
                Type = type,
                TableName = name,
                Alias = SQLBuilderHelper.BuildAlias(alias)
            };

            return (source, rawName);
        }
    }
}
using Dapper;
using SQLQueryBuilder.Enum;
using SQLQueryBuilder.Interface;
using SQLQueryBuilder.Model.ASTModel;
using SQLQueryBuilder.Model.SQLBuilderModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace SQLQueryBuilder.SQLBuilder
{
    public class Query<Root> : IQuerySelect, IQuery, IQueryFilter
    {
        private int _index = 0;
        private SQLInfoModel _sqlInfo;
        public Query(string alias)
        {
            Reset();
            _sqlInfo = new SQLInfoModel()
            {
                Root = CreateTable<Root>(alias),
            };
        }
        public IQuery Select(string colums)
        {
            _sqlInfo.SelectedColumns = colums;
            return this;
        }
        public IQuery MapField(params (string columnName, string alias)[] maps)
        {
            _sqlInfo.AddColumnMaps(maps);
            return this;
        }
        public IQuery Join<T>(string tableAlias, string join, JoinTypeEnum joinType = JoinTypeEnum.INNER)
        {
            var newJoin = new JoinInfoModel()
            {
                Table = CreateTable<T>(tableAlias),
                On = join
            };
            _sqlInfo.Joins.Add(newJoin);
            return this;
        }
        public IQueryFilter GroupBy(string groupBy = null)
        {
            if(groupBy != null)
            {
                var colName = _sqlInfo.GetMappedColumnName(groupBy.Trim());
                _sqlInfo.GroupBy = $"{colName}";
            }
            return this;
        }
        public IQueryFilter OrderBy(string orderBy = null, OrderTypeEnum order = OrderTypeEnum.DESC)
        {
            if(orderBy != null)
            {
                var colName = _sqlInfo.GetMappedColumnName(orderBy.Trim());
                _sqlInfo.OrderBy = $"{colName} {order.ToString()}";
            }
            return this;
        }
        public IQueryFilter Where(ExpressionModel expr = null)
        {
            if (expr == null) return this;

            var parameters = new DynamicParameters();
            string Visit(ExpressionModel expr)
            {
                if (expr is BinaryExpr binary)
                {
                    var left = Visit(binary.Left);
                    var right = Visit(binary.Right);
                    return $"({left} {binary.Op} {right})";
                }
                else if (expr is ComparisonExpr comparison)
                {
                    var columnName = _sqlInfo.GetMappedColumnName(comparison.Identifier);

                    var param = $"@p{_index++}";
                    var value = FormatValue(comparison.Op, comparison.Value);
                    var op = NormalizeOperator(comparison.Op);

                    parameters.Add(param, value);
                    return $"{columnName} {op} {param}";
                }

                throw new Exception("Unknown expression");
            }
            _sqlInfo.Where = Visit(expr);
            _sqlInfo.Parameters = parameters;
            return this;
        }
        public (string, DynamicParameters) Build()
        {
            var parts = new List<string>();

            parts.Add($"SELECT {_sqlInfo.SelectedColumns}");
            parts.Add($"FROM {_sqlInfo.Root.Name} AS {_sqlInfo.Root.Alias}");

            if (_sqlInfo.Joins.Count > 0)
            {
                parts.AddRange(_sqlInfo.Joins.Select(j =>
                    $"{j.JoinType} JOIN {j.Table.Name} AS {j.Table.Alias} ON {j.On}"
                ));
            }

            if (!string.IsNullOrWhiteSpace(_sqlInfo.Where))
                parts.Add($"WHERE {_sqlInfo.Where}");

            if (!string.IsNullOrWhiteSpace(_sqlInfo.GroupBy))
                parts.Add($"GROUP BY {_sqlInfo.GroupBy}");

            if (!string.IsNullOrWhiteSpace(_sqlInfo.OrderBy))
                parts.Add($"ORDER BY {_sqlInfo.OrderBy}");

            var sql = string.Join(" ", parts);

            return (sql, _sqlInfo.Parameters);
        }
        private string GetTableName(Type TableType) => TableType.GetCustomAttribute<TableAttribute>()?.Name ?? TableType.Name;
        private void Reset()
        {
            _sqlInfo = null;
            _index = 0;
        }
        private object FormatValue(string op, object value)
        {
            if (op == ":")
            {
                return $"%{value}%";
            }

            return value;
        }
        private string NormalizeOperator(string op)
        {
            return op switch
            {
                ":" => "LIKE",
                _ => op
            };
        }
        private TableRefModel CreateTable<T>(string alias)
        {
            var type = typeof(T);
            var tableName = GetTableName(type);

            return new TableRefModel()
            {
                Name = tableName,
                Type = type,
                Alias = alias
            };
        }
    }
}

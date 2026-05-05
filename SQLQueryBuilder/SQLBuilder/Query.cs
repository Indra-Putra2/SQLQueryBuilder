using Dapper;
using SQLQueryBuilder.Model.ASTModel;
using SQLQueryBuilder.Model.Enum;
using SQLQueryBuilder.Model.Interface;
using SQLQueryBuilder.Model.SQLBuilderModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace SQLQueryBuilder.SQLBuilder
{
    public class Query : IQueryStart, IQuery, IQueryAfterWhere
    {
        private int _index = 0;
        private SQLInfo _sqlInfo;

        public IQuery From<T>(string tableAlias)
        {
            _sqlInfo = new SQLInfo()
            {
                Root = CreateTable<T>(tableAlias)
            };
            return this;
        }
        public IQuery Select(string colums = null)
        {
            _sqlInfo.SelectedColumns = colums;
            return this;
        }
        public IQuery Join<T>(string tableAlias, string join, JoinTypeEnum joinType = JoinTypeEnum.INNER)
        {
            var newJoin = new JoinInfo()
            {
                Table = CreateTable<T>(tableAlias),
                On = join
            };
            _sqlInfo.Joins.Add(newJoin);
            return this;
        }
        public IQuery GroupBy(string groupBy)
        {
            _sqlInfo.GroupBy = groupBy;
            return this;
        }
        public IQuery OrderBy(string orderBy, OrderTypeEnum order = Model.Enum.OrderTypeEnum.DESC)
        {
            _sqlInfo.OrderBy = $"{orderBy.Trim()} {order.ToString()}";
            return this;
        }
        public IQueryAfterWhere Where(Expr expr)
        {
            var parameters = new DynamicParameters();

            string Visit(Expr expr)
            {
                if (expr is BinaryExpr binary)
                {
                    var left = Visit(binary.Left);
                    var right = Visit(binary.Right);
                    return $"({left} {binary.Op} {right})";
                }
                else if (expr is ComparisonExpr comparison)
                {
                    var column = GetColumnName(_sqlInfo.Root.Type, comparison.Identifier);
                    if (column == null)
                    {
                        throw new InvalidFilterCriteriaException($"Cannot Find Column With The Name {comparison.Identifier}");
                    }
                    var param = $"@p{_index++}";
                    var value = FormatValue(comparison.Op, comparison.Value);
                    var op = NormalizeOperator(comparison.Op);

                    parameters.Add(param, value);
                    Console.WriteLine($"param: {column} val: {value}");
                    return $"{_sqlInfo.Root.Alias}.{column} {op} {param}";
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

            if (!string.IsNullOrEmpty(_sqlInfo.Where))
                parts.Add($"WHERE {_sqlInfo.Where}");

            if (!string.IsNullOrEmpty(_sqlInfo.GroupBy))
                parts.Add($"GROUP BY {_sqlInfo.GroupBy}");

            if (!string.IsNullOrEmpty(_sqlInfo.OrderBy))
                parts.Add($"ORDER BY {_sqlInfo.OrderBy}");

            var sql = string.Join("\n", parts);

            return (sql, _sqlInfo.Parameters);
        }
        private string GetTableName(Type TableType) => TableType.GetCustomAttribute<TableAttribute>()?.Name ?? TableType.Name;
        private string? GetColumnName(Type classType, string propertyName)
        {
            var prop = classType.GetProperty(propertyName);
            if (prop == null) return null;
            return prop.GetCustomAttribute<ColumnAttribute>()?.Name ?? prop.Name;
        }
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
        private TableRef CreateTable<T>(string alias)
        {
            var type = typeof(T);
            var tableName = GetTableName(type);

            return new TableRef()
            {
                Name = tableName,
                Type = type,
                Alias = alias
            };
        }
    }
}

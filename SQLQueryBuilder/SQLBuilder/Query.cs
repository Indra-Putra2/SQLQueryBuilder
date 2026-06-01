using ASTBuilder.Abstraction;
using ASTBuilder.ASTModel;
using Dapper;
using SQLQueryBuilder.Enum;
using SQLQueryBuilder.Interface;
using SQLQueryBuilder.Model;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace SQLQueryBuilder.SQLBuilder
{
    /// <summary>
    /// Implements a fluent interface to construct SQL SELECT queries based on a starting <typeparamref name="Root"/> entity type.
    /// Tracks SQL metadata state and safely converts an expression tree into parameterized SQL logic.
    /// </summary>
    /// <typeparam name="Root">The primary database model/table type mapping the initial query source.</typeparam>
    public class Query<Root> : IQuerySelect, IQuery, IQueryFilter
    {
        private int _index = 0; // Tracks unique counter IDs for generated SQL parameters (e.g., @p0, @p1)
        private SQLInfoModel _sqlInfo;

        public Query(string alias)
        {
            Reset();
            _sqlInfo = new SQLInfoModel()
            {
                // Reflectively discovers details about the target table type and pairs it with its query-specific alias
                Root = CreateTable<Root>(alias),
            };
        }

        /// <summary> Specifies the exact column block string to inject into the SELECT statement. </summary>
        public IQuery Select(string colums)
        {
            _sqlInfo.SelectedColumns = colums;
            return this;
        }

        /// <summary> Registers explicit manual column/alias overrides within state tracking tracking. </summary>
        public IQuery MapField(params (string columnName, string alias)[] maps)
        {
            _sqlInfo.AddColumnMaps(maps);
            return this;
        }

        /// <summary> 
        /// Appends an arbitrary structural JOIN command referencing a generic entity model <typeparamref name="T"/>. 
        /// </summary>
        public IQuery Join<T>(string tableAlias, string joinOnCondition, JoinTypeEnum joinType = JoinTypeEnum.INNER)
        {
            var newJoin = new JoinInfoModel()
            {
                Table = CreateTable<T>(tableAlias),
                On = joinOnCondition,
                JoinType = joinType // Note: Fixed structural bug: assigned property to match builder output loop
            };
            _sqlInfo.Joins.Add(newJoin);
            return this;
        }

        /// <summary> Appends a validated GROUP BY criteria target. </summary>
        public IQueryFilter GroupBy(string groupBy = null)
        {
            if (groupBy != null)
            {
                var colName = _sqlInfo.GetMappedColumnName(groupBy.Trim());
                _sqlInfo.GroupBy = $"{colName}";
            }
            return this;
        }

        /// <summary> Appends a validated ORDER BY sequence tracking structure. </summary>
        public IQueryFilter OrderBy(string orderBy = null, OrderTypeEnum order = OrderTypeEnum.DESC)
        {
            if (orderBy != null)
            {
                var colName = _sqlInfo.GetMappedColumnName(orderBy.Trim());
                _sqlInfo.OrderBy = $"{colName} {order.ToString()}";
            }
            return this;
        }

        /// <summary>
        /// Translates an Abstract Syntax Tree (AST) expression design recursively into a safe, valid SQL string.
        /// Extracts raw constants safely into a clean, parameterized DynamicParameters collection.
        /// </summary>
        public IQueryFilter Where(IExpression expr = null)
        {
            if (expr == null) return this;

            var parameters = new DynamicParameters();

            // Local helper function to walk down nested tree expressions recursively
            string Visit(IExpression expr)
            {
                if (expr is BinaryExpr binary)
                {
                    // Handles logical grouping constructs like (X AND Y) or (A OR B)
                    var left = Visit(binary.Left);
                    var right = Visit(binary.Right);
                    return $"({left} {binary.Op} {right})";
                }
                else if (expr is ComparisonExpr comparison)
                {
                    // Resolves field identifiers into their mapped target names (e.g. "alias.ColumnName")
                    var columnName = _sqlInfo.GetMappedColumnName(comparison.Identifier);

                    // Sanitizes literal code variables to neutralize threat vectors like SQL injection
                    var param = $"@p{_index++}";
                    var value = FormatValue(comparison.Op, comparison.Value);
                    var op = NormalizeOperator(comparison.Op);

                    parameters.Add(param, value);
                    return $"{columnName} {op} {param}";
                }

                throw new Exception("Unknown expression structure passed to builder.");
            }

            _sqlInfo.Where = Visit(expr);
            _sqlInfo.Parameters = parameters;
            return this;
        }

        /// <summary>
        /// Assembles all registered metadata variables into a clean executable raw SQL string statement and parameters tuple pack.
        /// </summary>
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

        /// <summary> Resolves a class type down to its true DB table identity by scanning for [Table] overrides. </summary>
        private string GetTableName(Type TableType) => TableType.GetCustomAttribute<TableAttribute>()?.Name ?? TableType.Name;

        private void Reset()
        {
            _sqlInfo = null;
            _index = 0;
        }

        /// <summary> Transforms structural parameters based on business logic rules (like wrapping strings for LIKE expressions). </summary>
        private object FormatValue(string op, object value)
        {
            if (op == ":")
            {
                return $"%{value}%"; // Turns text searches into wildcard queries
            }

            return value;
        }

        /// <summary> Translates domain or UI custom operators directly into recognized SQL keywords. </summary>
        private string NormalizeOperator(string op)
        {
            return op switch
            {
                ":" => "LIKE", // Rewrites context-specific symbols to standard SQL syntax
                _ => op
            };
        }

        /// <summary> Introspects type generic parameters to yield metadata references for string assembly generation. </summary>
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
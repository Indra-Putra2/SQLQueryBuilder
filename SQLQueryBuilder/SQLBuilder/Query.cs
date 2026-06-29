using ASTBuilder.Core.ASTModel;
using Dapper;
using SQLQueryBuilder.Enum;
using SQLQueryBuilder.Interface;
using SQLQueryBuilder.Model;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace SQLQueryBuilder.Builder
{
    /// <summary>
    /// Implements a fluent interface to construct SQL SELECT queries based on a starting <typeparamref name="Root"/> entity type.
    /// Tracks SQL metadata state and safely converts an expression tree into parameterized SQL logic.
    /// </summary>
    /// <typeparam name="Root">The primary database model/table type mapping the initial query source.</typeparam>
    public class Query<Root> : IQuerySelect, IQuery, IQueryFilter
    {
        private SQLInfoModel _sqlInfo;
        private SQLSelectParser _selectParser = new SQLSelectParser();
        private SQLJoinParser _joinParser = new SQLJoinParser();
        private ExpressionModel _rawWhere;
        private string _cachedSqlBase;
        private bool _isBaseCompiled;

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
        public IQuery Select(string columns)
        {
            _sqlInfo._rawSelectedColumns = columns;
            _sqlInfo.ParsedSelected = _selectParser.Parse(columns);
            return this;
        }

        /// <summary> 
        /// Appends an arbitrary structural JOIN command referencing a generic entity model <typeparamref name="T"/>. 
        /// </summary>
        public IQuery Join<T>(string tableAlias, string joinOnCondition, JoinTypeEnum joinType = JoinTypeEnum.INNER)
        {
            var join = new JoinInfoModel()
            {
                Table = CreateTable<T>(tableAlias),
                On = joinOnCondition,
                JoinType = joinType
            };

            _sqlInfo.Joins.Add(join);
            return this;
        }

        /// <summary> Appends a validated GROUP BY criteria target. </summary>
        public IQueryFilter GroupBy(string groupBy = null)
        {
            _sqlInfo.GroupBy = groupBy?.Trim() ?? string.Empty;
            return this;
        }

        /// <summary> Appends a validated ORDER BY sequence tracking structure. </summary>
        public IQueryFilter OrderBy(OrderByModel order)
        {
            _sqlInfo.OrderBy = order.Column;
            _sqlInfo.OrderType = order.OrderType;
            return this;
        }

        /// <summary>
        /// Translates an Abstract Syntax Tree (AST) expression design recursively into a safe, valid SQL string.
        /// Extracts raw constants safely into a clean, parameterized DynamicParameters collection.
        /// </summary>
        public IQueryFilter Where(ExpressionModel expr = null)
        {
            _rawWhere = expr;
            return this;
        }

        /// <summary>
        /// Assembles all registered metadata variables into a clean executable raw SQL string statement and parameters tuple pack.
        /// </summary>
        public IQueryFilter CompileSqlBase()
        {
            if (_isBaseCompiled) return this;

            _sqlInfo.BuildValidColums(); // Now lightning fast due to SchemaCache!
            _sqlInfo.validateSelected(_sqlInfo.ParsedSelected);

            var parts = new List<string>
        {
            "WITH CTEWrapper AS (",
            $"SELECT {_sqlInfo._rawSelectedColumns}",
            $"FROM {_sqlInfo.Root.Name} AS {_sqlInfo.Root.Alias}"
        };

            if (_sqlInfo.Joins.Count > 0)
            {
                foreach(var join in _sqlInfo.Joins)
                {
                    _sqlInfo.ValidateJoin(_joinParser.Parse(join.On));
                }
                
                parts.AddRange(_sqlInfo.Joins.Select(j =>
                    $"{j.JoinType} JOIN {j.Table.Name} AS {j.Table.Alias} ON {j.On}"
                ));
            }
            parts.Add(") SELECT * FROM CTEWrapper");

            _cachedSqlBase = string.Join(" ", parts);
            _isBaseCompiled = true;

            return this;
        }

        public (string sql, DynamicParameters param) Build()
        {
            // 1. Ensure structural query base components are generated/cached
            CompileSqlBase();

            // 2. Parse variable dynamic segments per invocation pass
            var (whereSql, parameters) = ParseExpression(_rawWhere);

            var dynamicParts = new List<string> { _cachedSqlBase };

            if (!string.IsNullOrWhiteSpace(whereSql))
                dynamicParts.Add($"WHERE {whereSql}");

            if (!string.IsNullOrWhiteSpace(_sqlInfo.GroupBy))
                dynamicParts.Add($"GROUP BY {_sqlInfo.GroupBy}");

            if (!string.IsNullOrWhiteSpace(_sqlInfo.OrderBy))
                dynamicParts.Add($"ORDER BY {_sqlInfo.OrderBy} {_sqlInfo.OrderType.ToString()}");

            return (string.Join("\n", dynamicParts), parameters);
        }

        /// <summary> Resolves a class type down to its true DB table identity by scanning for [Table] overrides. </summary>
        private string GetTableName(Type TableType) => TableType.GetCustomAttribute<TableAttribute>()?.Name ?? TableType.Name;

        private void Reset()
        {
            _sqlInfo = null;
            _cachedSqlBase = null;
            _isBaseCompiled = false;
            _rawWhere = null;
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

        private (string where, DynamicParameters param) ParseExpression(ExpressionModel exp)
        {
            if(exp == null) return ("",new DynamicParameters());

            var parameters = new DynamicParameters();
            var index = 0;
            // Local helper function to walk down nested tree expressions recursively
            string Visit(ExpressionModel expr)
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
                    // Sanitizes literal code variables to neutralize threat vectors like SQL injection
                    var param = $"@p{index++}";
                    var column = _sqlInfo.ValidateColumn(comparison.Identifier.Trim());
                    var op = NormalizeOperator(comparison.Op);
                    var value = FormatValue(comparison.Op, comparison.Value);

                    parameters.Add(param, value);
                    return $"{column} {op} {param}";
                }
                throw new Exception("Unknown expression structure passed to builder.");
            }

            var sql = Visit(exp);
            return (sql, parameters);
        }
    }
}
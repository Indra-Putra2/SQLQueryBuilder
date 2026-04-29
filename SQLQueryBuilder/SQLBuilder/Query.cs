using Dapper;
using SQLQueryBuilder.Model.ASTModel;
using SQLQueryBuilder.Model.Enum;
using SQLQueryBuilder.Model.Interface;
using SQLQueryBuilder.Model.SQLBuilderModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;

namespace SQLQueryBuilder.SQLBuilder
{
    public class Query<TPrime> : IQuerySelect<TPrime>, IQueryAfterSelect<TPrime>
    {
        private SQLInfo _sqlInfo;
        private int _count = 0;
        private int _index = 0;
        public IQueryAfterSelect<TPrime> Select(Expression<Func<TPrime, object>> selectExpr = null)
        {
            Reset();
            var type = typeof(TPrime);
            var tableName = GetTableName(type);
            var columns = GetColumns(selectExpr);

            _sqlInfo = new()
            {
                Root = new TableRef()
                {
                    Type = type,
                    Name = tableName,
                    Alias = $"t{_count++}",
                },
            };
            _sqlInfo.SelectedColumns.UnionWith(columns);
            return this;
        }
        public IQueryAfterSelect<TPrime> Join<TJoin>(
            Expression<Func<TPrime, TJoin, bool>> on,
            Expression<Func<TJoin, object>> select = null,
            Expression<Func<TPrime, object>> map = null,
            JoinType joinType = JoinType.INNER)
        {

            var (leftExp, rightExp) = GetOnClause(on);

            var leftType = leftExp.type;
            var rightType = rightExp.type;

            bool leftRoot = IsTableRoot(leftType);
            bool RightRoot = IsTableRoot(rightType);

            var joinTableExist = false;

            TableRef fromTable;
            TableRef toTable;
            string fromColumn;
            string toColumn;

            if (leftRoot && !RightRoot)
            {
                fromTable = ResolveRootTable(leftType);
                (toTable, joinTableExist) = ResolveOrCreateTable(rightType);
                fromColumn = leftExp.column;
                toColumn = rightExp.column;
            }
            else if (!leftRoot && RightRoot)
            {
                fromTable = ResolveRootTable(rightType);
                (toTable, joinTableExist) = ResolveOrCreateTable(leftType);
                fromColumn = rightExp.column;
                toColumn = leftExp.column;
            }
            else
            {
                throw new Exception($"Table {typeof(TJoin).Name} already joined. Use aliasing for multiple joins.");
            }

            JoinInfo joinInfo = null;

            if (!joinTableExist)
            {
                joinInfo = new JoinInfo()
                {
                    From = fromTable,
                    To = toTable,
                    On = new JoinCondition()
                    {
                        FromColumn = fromColumn,
                        ToColumn = toColumn,
                    },
                    JoinType = joinType
                };
            }
            else
            {
                joinInfo = _sqlInfo.Joins.First(j => j.To.Type == typeof(TJoin));
                if (joinInfo.JoinType != joinType)
                {
                    throw new Exception(
                        $"Conflicting join types for {typeof(TJoin).Name}: " +
                        $"{joinInfo.JoinType} vs {joinType}");
                }
            }
            var columns = GetColumns(select);
            var columnMap = GetColumns(map);

            joinInfo!.SelectedColumns.UnionWith(columns);
            joinInfo!.AddMaps(columnMap);

            if (!joinTableExist) _sqlInfo.Joins.Add(joinInfo);
            return this;
        }
        public IQueryAfterSelect<TPrime> Where(Expr expr)
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
                    var column = GetColumnName(typeof(TPrime), comparison.Identifier);
                    if (column == null)
                    {
                        throw new InvalidFilterCriteriaException($"Cannot Find Column With The Name {comparison.Identifier}");
                    }
                    var param = $"@p{_index++}";
                    var value = FormatValue(comparison.Op, comparison.Value);
                    var op = NormalizeOperator(comparison.Op);

                    parameters.Add(param, value);
                    Console.WriteLine($"param: {column} val: {value}");
                    return $"{column} {op} {param}";
                }

                throw new Exception("Unknown expression");
            }
            _sqlInfo.Where = Visit(expr);
            _sqlInfo.Parameters = parameters;
            return this;
        }
        public (string sql, DynamicParameters parameters) Build()
        {
            var mapValidation = _sqlInfo.Joins.FirstOrDefault(j => j.ColumnsMaps.Count > j.SelectedColumns.Count);
            if (mapValidation != null)
            {
                throw new InvalidOperationException(
                    $"Mapping mismatch: Received {mapValidation.ColumnsMaps.Count} mapping targets, " +
                    $"but only {mapValidation.SelectedColumns.Count} source columns were selected. " +
                    "The number of mapping aliases must be less than or equal to the source columns."
                );
            }

            var mainColumns = string.Join(",\n", _sqlInfo.SelectedColumns.Select(c => { return $"{_sqlInfo.Root.Alias}.{c}"; }));

            var joinsColumns = _sqlInfo.Joins.Select(j =>
            {
                var final = j.GetMappedColumns().Select(c => $"{j.To.Alias}.{c}");
                return final;
            });

            var finalJoinsColums = string.Join(",\n", joinsColumns.Select(j => string.Join(",\n", j.Select(c => c))));

            var selectClause = $"SELECT\n{mainColumns},\n{finalJoinsColums}\nFROM {_sqlInfo.Root.Name} AS {_sqlInfo.Root.Alias}";

            string joinClause = string.Join("\n", _sqlInfo.Joins.Select(j =>
            {
                return $"{j.JoinType} JOIN {j.To.Name} AS {j.To.Alias} " +
                       $"ON {j.From.Alias}.{j.On.FromColumn} = {j.To.Alias}.{j.On.ToColumn}";
            }));

            var whereClause = $"WHERE {_sqlInfo.Where}";

            string sql = $"{selectClause}\n{joinClause}\n{whereClause}";
            DynamicParameters parameters = new();
            return (sql, parameters);
        }
        private List<string> GetColumns(Expression expression)
        {
            var columns = new List<string>();
            void Visit(Expression expression)
            {
                if (expression is LambdaExpression lambda)
                {
                    Visit(lambda.Body);
                }
                else if (expression is NewExpression newExpressions)
                {
                    for (var i = 0; i < newExpressions.Arguments.Count; i++)
                    {
                        var (_, name) = GetMemberInfo(newExpressions.Arguments[i]);
                        columns.Add(name);
                    }
                }
            }
            Visit(expression);
            return columns;
        }
        private ((Type type, string column), (Type type, string column)) GetOnClause(Expression expression)
        {
            (Type type, string colName) left = default;
            (Type type, string colName) right = default;
            bool assigned = false;

            void Visit(Expression exp)
            {
                if (exp is LambdaExpression lambda)
                {
                    Visit(lambda.Body);
                }
                else if (exp is UnaryExpression unary)
                {
                    Visit(unary.Operand);
                }
                else if (exp is BinaryExpression binary)
                {
                    left = GetMemberInfo(binary.Left);
                    right = GetMemberInfo(binary.Right);
                    assigned = true;
                }
            }

            Visit(expression);

            if (!assigned)
                throw new InvalidOperationException("No binary expression found in ON clause.");

            return (left, right);
        }
        private (Type owner, string name) GetMemberInfo(Expression expression)
        {
            if (expression is MemberExpression member)
            {
                var colName = GetColumnName(member.Member.DeclaringType, member.Member.Name);
                return (member.Member.DeclaringType, colName);
            }
            return (null, string.Empty);
        }
        private string GetTableName(Type TableType) => TableType.GetCustomAttribute<TableAttribute>()?.Name ?? TableType.Name;
        private string? GetColumnName(Type classType, string propertyName)
        {
            var prop = classType.GetProperty(propertyName);
            if (prop == null) return null;
            return prop.GetCustomAttribute<ColumnAttribute>()?.Name ?? prop.Name;
        }
        private TableRef ResolveRootTable(Type type)
        {
            if (_sqlInfo.Root.Type != type)
                throw new Exception("Table not found in query");

            return _sqlInfo.Root;
        }
        private (TableRef table, bool exist) ResolveOrCreateTable(Type type)
        {
            var exist = _sqlInfo.Joins.Where(j => j.To.Type == type).Select(j => j.To).FirstOrDefault();
            if (exist != null)
            {
                return (exist, true);
            }

            return (new TableRef
            {
                Type = type,
                Name = GetTableName(type),
                Alias = $"t{_count++}"
            }, false);
        }
        private bool IsTableRoot(Type type)
        {
            if (_sqlInfo.Root.Type == type)
                return true;

            return false;
        }
        private void Reset()
        {
            _sqlInfo = null;
            _count = 0;
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
    }
}

using Dapper;
using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.Enum;
using SQLQueryBuilder.Model.Interface;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;

namespace SQLQueryBuilder.SQLBuilder
{
    public class Query<TPrime> : IQuerySelect<TPrime>, IQueryAfterSelect<TPrime>
    {
        private SQLInfo _sqlInfo;
        public IQueryAfterSelect<TPrime> Select(Expression<Func<TPrime, object>> selectExpr = null, string tableAlias = null)
        {
            var tableName = GetTableName(typeof(TPrime));
            var alias = tableAlias ?? tableName;
            var columns = GetColumns(selectExpr);

            _sqlInfo = new()
            {
                TableInfo = new TableInfo()
                {
                    Name = tableName,
                    Alias = alias
                },
                SelectedColumn = columns
            };
            return this;
        }
        public IQueryAfterSelect<TPrime> Join<TJoin>(
            Expression<Func<TPrime, TJoin, bool>> on,
            Expression<Func<TJoin, object>> select,
            JoinType joinType = JoinType.INNER,
            string tableAlias = null)
        {
            (var leftName, var rightName) = GetOnClause(on);
            var newColumns = GetColumns(select);

            _sqlInfo.JoinInfo ??= new HashSet<JoinInfo>();

            // 1. Check if we already have a join for these tables with this type
            var existingJoin = _sqlInfo.JoinInfo.FirstOrDefault(j => j.IsMatch(leftName, rightName, joinType, tableAlias));

            if (existingJoin != null)
            {
                // 2. Duplicate found: Just merge the columns into the existing pool
                existingJoin.UnionColumns(newColumns);
            }
            else
            {
                // 3. New join: Create it and assign new aliases
                var joinInfo = new JoinInfo()
                {
                    TableName = typeof(TJoin).Name,
                    Alias = tableAlias,
                    LeftColumn = leftName,
                    RightColumn = rightName,
                    SelectedColumns = newColumns,
                    JoinType = joinType
                };
                _sqlInfo.JoinInfo.Add(joinInfo);
            }

            return this;
        }
        public IQueryAfterSelect<TPrime> Where(Expr expr)
        {
            return this;
        }

        public (string sql, DynamicParameters parameters) Build()
        {
            //Primary Table
            string primaryTable = _sqlInfo.TableInfo.Name;
            string primaryTableAlias = _sqlInfo.TableInfo.Alias;
            string fromClause = primaryTableAlias == null ? $"{primaryTable}" : $"{primaryTable} AS {primaryTableAlias}";
            string primaryColumns = 
                string.Join(",", _sqlInfo.SelectedColumn.Select(c => { return $"\n{primaryTableAlias??primaryTable}.{c}"; }));

            string joinColumns = string.Join(",", _sqlInfo.JoinInfo.Select(j =>
            {
                string name = j.Alias ?? j.TableName;
                return string.Join(",\n",j.SelectedColumns.Select(c => $"{name}.{c}"));
            }));

            // Inside Build()
            string joinClause = string.Join("\n", _sqlInfo.JoinInfo.Select(j =>
            {
                string targetTable = j.Alias == null ? j.TableName : $"{j.TableName} AS {j.Alias}";
                string targetIdentifier = j.Alias ?? j.TableName;
                string sourceIdentifier = _sqlInfo.TableInfo.Alias ?? _sqlInfo.TableInfo.Name;

                // Based on (b, r) => b.id == r.BrandID:
                // b.id is j.LeftColumn, r.BrandID is j.RightColumn
                return $"{j.JoinType} JOIN {targetTable} ON {targetIdentifier}.{j.LeftColumn} = {sourceIdentifier}.{j.RightColumn}";
            }));

            string sql = $"SELECT {primaryColumns},\n{joinColumns}\nFROM {fromClause}\n{joinClause}";
            DynamicParameters parameters = new();
            return (sql, parameters);
        }
        private HashSet<string> GetColumns(Expression expression)
        {
            var columns = new HashSet<string>();
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
                        columns.Add(GetMemberName(newExpressions.Arguments[i]));
                    }
                }
            }
            Visit(expression);
            return columns;
        }
        private (string left, string right) GetOnClause(Expression expression)
        {
            string left = string.Empty;
            string right = string.Empty;

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
                    left = GetMemberName(binary.Left);
                    right = GetMemberName(binary.Right);
                }
            }

            Visit(expression);
            return (left, right);
        }
        private string GetMemberName(Expression expression)
        {
            if (expression is MemberExpression member)
            {
                return GetColumnName(member.Member.DeclaringType, member.Member.Name);
            }
            return string.Empty;
        }
        private string GetTableName(Type TableType) => TableType.GetCustomAttribute<TableAttribute>()?.Name ?? TableType.Name;
        private string GetColumnName(Type classType, string propertyName)
        {
            var prop = classType.GetProperty(propertyName);
            return prop.GetCustomAttribute<ColumnAttribute>()?.Name ?? prop.Name;
        }
    }
}

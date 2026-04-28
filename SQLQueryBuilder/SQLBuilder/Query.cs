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
            
            (var left, var right) = GetOnClause(on);
            var newColumns = GetColumns(select);

            JoinOnInfo leftInfo = null;
            JoinOnInfo rightInfo = null;

            var leftIsPrime = left.type == typeof(TPrime);
            var rightIsPrime = right.type == typeof(TPrime);

            if (leftIsPrime && !rightIsPrime)
            {
                leftInfo = BuildJoinInfo(left.type,_sqlInfo.TableInfo.Name,left.colName,_sqlInfo.TableInfo.Alias);
                rightInfo = BuildJoinInfo(right.type,GetTableName(right.type),right.colName,tableAlias);
            }
            else if (!leftIsPrime && rightIsPrime)
            {
                leftInfo = BuildJoinInfo(left.type, GetTableName(left.type), left.colName, tableAlias);
                rightInfo = BuildJoinInfo(right.type, _sqlInfo.TableInfo.Name, right.colName,_sqlInfo.TableInfo.Alias);
            }
            else
            {
                throw new InvalidOperationException("Invalid join expression. Must join TPrime with TJoin.");
            }

            _sqlInfo.JoinInfo ??= new HashSet<JoinInfo>();

            // 1. Check if we already have a join for these tables with this type
            var existingJoin = _sqlInfo.JoinInfo.FirstOrDefault(j => j.IsMatch(leftInfo, rightInfo, joinType, tableAlias));

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
                    LeftColumn = leftInfo,
                    RightColumn = rightInfo,
                    SelectedColumns = newColumns,
                    JoinType = joinType
                };
                _sqlInfo.JoinInfo.Add(joinInfo);
            }

            return this;
        }
        private JoinOnInfo BuildJoinInfo(Type type, string tableName, string colName, string alias)
        {
            return new JoinOnInfo()
            {
                TableType = type,
                TableName = tableName,
                ColumnName = colName,
                Alias = alias
            };
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

            string joinColumns = string.Join(",\n", _sqlInfo.JoinInfo.Select(j =>
            {
                string name = j.LeftColumn.Alias ?? j.LeftColumn.ColumnName;
                return string.Join(",\n",j.SelectedColumns.Select(c => $"{name}.{c}"));
            }));

            // Inside Build()
            string joinClause = string.Join("\n", _sqlInfo.JoinInfo.Select(j =>
            {
                var tablePart = string.IsNullOrWhiteSpace(j.LeftColumn.Alias)
                    ? j.LeftColumn.TableName
                    : $"{j.LeftColumn.TableName} AS {j.LeftColumn.Alias}";

                var leftAlias = string.IsNullOrWhiteSpace(j.LeftColumn.Alias)
                    ? j.LeftColumn.TableName
                    : j.LeftColumn.Alias;

                var rightAlias = string.IsNullOrWhiteSpace(j.RightColumn.Alias)
                    ? j.RightColumn.TableName
                    : j.RightColumn.Alias;

                return $"{j.JoinType} JOIN {tablePart} ON {leftAlias}.{j.LeftColumn.ColumnName} = {rightAlias}.{j.RightColumn.ColumnName}";
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
        private ((Type type, string colName), (Type type, string colName)) GetOnClause(Expression expression)
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
        private string GetMemberName(Expression expression)
        {
            if (expression is MemberExpression member)
            {
                return GetColumnName(member.Member.DeclaringType, member.Member.Name);
            }
            return string.Empty;
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
        private string GetColumnName(Type classType, string propertyName)
        {
            var prop = classType.GetProperty(propertyName);
            return prop.GetCustomAttribute<ColumnAttribute>()?.Name ?? prop.Name;
        }
    }
}

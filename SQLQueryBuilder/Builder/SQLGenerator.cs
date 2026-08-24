using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.SQL_Expression;
using SQLQueryBuilder.SQLEnum;
using System.Text;

namespace SQLQueryBuilder.Builder
{
    internal class SQLGenerator
    {
        private readonly StringBuilder _query = new();
        private int _indent = 0;
        #region SQL Builder
        private void BuildSelect(SQLQuery sqlQuery)
        {
            _query.Append("SELECT");
            _indent++;
            for (int i = 0; i < sqlQuery.Select.Count; i++)
            {
                if (i > 0)
                    _query.Append(",");
                NewLine();
                IndentedLine();
                WriteExpression(sqlQuery.Select[i]);
            }

            _indent--;
        }
        private void BuildFrom(SQLQuery sqlQuery)
        {
            NewLine();
            _query.Append("FROM ").Append(string.Join(".", sqlQuery.From.TableName));
            if (sqlQuery.From.Alias != null)
                _query.Append($" AS {sqlQuery.From.Alias}");
        }
        private void BuildJoins(SQLQuery sqlQuery)
        {
            if (sqlQuery.Joins.Count == 0) return;

            foreach (var join in sqlQuery.Joins)
            {
                string joinType = join.JoinType switch
                {
                    SQLJoinType.INNER => $"INNER JOIN ",
                    SQLJoinType.LEFT => $"LEFT JOIN ",
                    SQLJoinType.RIGHT => $"RIGHT JOIN ",
                    SQLJoinType.FULL => $"FULL JOIN ",
                    _ => $"INNER JOIN "
                };

                NewLine();
                _query.Append(joinType);
                _query.Append(string.Join(".", join.Table.TableName));

                if (join.Table.Alias != null)
                    _query.Append($" AS {join.Table.Alias}");

                _query.Append(" ON ");
                WriteExpression(join.Condition);
            }
        }
        private void BuildHaving(SQLQuery sqlQuery)
        {
            if (sqlQuery.Having == null) return;

            NewLine();
            _query.Append("HAVING ");
            WriteExpression(sqlQuery.Having);
        }
        private void BuildWhere(SQLQuery sqlQuery)
        {
            if (sqlQuery.Where == null) return;

            NewLine();
            _query.Append("WHERE ");
            WriteExpression(sqlQuery.Where);
        }
        private void BuildGroupBy(SQLQuery sqlQuery)
        {
            if (sqlQuery.GroupBy == null) return;

            NewLine();
            _query.Append("GROUP BY ");
            for (int i = 0; i < sqlQuery.GroupBy.Count; i++)
            {
                if (i > 0)
                    _query.Append(", ");

                WriteExpression(sqlQuery.GroupBy[i]);
            }
        }
        private void BuildOrderBy(SQLQuery sqlQuery)
        {
            if (sqlQuery.OrderBy == null) return;

            NewLine();
            _query.Append("ORDER BY ");
            for (int i = 0; i < sqlQuery.OrderBy.Count; i++)
            {
                if (i > 0)
                    _query.Append(", ");

                WriteExpression(sqlQuery.OrderBy[i]);
            }
        }
        public void BuildBaseQuery(SQLQuery sqlQuery)
        {
            BuildSelect(sqlQuery);
            BuildFrom(sqlQuery);
            BuildJoins(sqlQuery);
        }
        public string BuildClauses(SQLQuery sqlQuery)
        {
            BuildWhere(sqlQuery);
            BuildGroupBy(sqlQuery);
            BuildHaving(sqlQuery);
            BuildOrderBy(sqlQuery);

            return _query.ToString();
        }

        public string Build() => _query.ToString();
        #endregion
        #region SQL Expression Writer
        private void WriteExpression(SQLExpression expression, bool parameterize = false)
        {
            switch (expression)
            {
                case ColumnExpression column:
                    WriteColumn(column);
                    break;

                case LiteralExpression literal:
                    WriteLiteral(literal);
                    break;

                case BinaryExpression binary:
                    WriteBinary(binary);
                    break;

                case UnaryExpression unary:
                    WriteUnary(unary);
                    break;

                case FunctionExpression function:
                    WriteFunction(function);
                    break;

                case CaseExpression @case:
                    WriteCase(@case);
                    break;

                case WildcardExpression wildcard:
                    WriteWildcard(wildcard);
                    break;

                case IsNullExpression isNull:
                    WriteIsNull(isNull);
                    break;

                case LikeExpression like:
                    WriteLike(like);
                    break;

                case InExpression @in:
                    WriteIn(@in);
                    break;

                case BetweenExpression between:
                    WriteBetween(between);
                    break;

                case SelectExpression select:
                    WriteSelectExpression(select);
                    break;

                case OrderByExpression orderBy:
                    WriteOrderByExpression(orderBy);
                    break;

                case ParenthesizedExpression parenthesized:
                    WriteParenthesized(parenthesized);
                    break;

                default:
                    throw new NotSupportedException(
                        $"SQL expression type '{expression.GetType().Name}' is not supported.");
            }
        }
        private void WriteColumn(ColumnExpression expression)
        {
            var table = expression.Table == null ? "" : string.Join(".", expression.Table);
            _query.Append(table).Append(".").Append(expression.Column);
        }
        private void WriteLiteral(LiteralExpression expression, bool parameterize = false)
        {
            switch (expression.Type)
            {
                case SQLLiteralType.Money:
                    _query.Append("$").Append(expression.Value);
                    break;
                case SQLLiteralType.BinaryLiteral:
                    _query.Append("0x").Append(expression.Value);
                    break;
                case SQLLiteralType.UnicodeString:
                    _query.Append("N").Append($"'{expression.Value}'");
                    break;
                case SQLLiteralType.String:
                case SQLLiteralType.Scientific:
                case SQLLiteralType.Datetime:
                    _query.Append($"'{expression.Value}'");
                    break;
                case SQLLiteralType.Null:
                    _query.Append("NULL");
                    break;
                case SQLLiteralType.Decimal:
                case SQLLiteralType.Integer:
                    _query.Append(expression.Value);
                    break;
            }
        }
        private void WriteBinary(BinaryExpression expression)
        {
            WriteExpression(expression.Left);
            string op = expression.Operator switch
            {
                //Logical
                SQLBinaryOperator.AND => "AND",
                SQLBinaryOperator.OR => "OR",

                //Arithmetic
                SQLBinaryOperator.Subtract => "-",
                SQLBinaryOperator.Add => "+",
                SQLBinaryOperator.Divide => "/",
                SQLBinaryOperator.Multiply => "*",
                SQLBinaryOperator.Modulo => "%",

                // Comparison
                SQLBinaryOperator.LessThan => "<",
                SQLBinaryOperator.LessThanOrEqual => "<=",
                SQLBinaryOperator.GreaterThan => ">",
                SQLBinaryOperator.GreaterThanOrEqual => ">=",
                SQLBinaryOperator.Equal => "=",
                SQLBinaryOperator.NotEqual => "!=",

                // Bitsiwe
                SQLBinaryOperator.BitwiseAnd => "&",
                SQLBinaryOperator.BitwiseOr => "|",
                SQLBinaryOperator.BitwiseXor => "^",
                SQLBinaryOperator.BitwiseLeftShift => "<<",
                SQLBinaryOperator.BitwiseRightShift => ">>",

                _ => throw new NotSupportedException($"Binary with the operator {expression.Operator} is not Supported")
            };

            _query.Append(' ').Append(op).Append(' ');

            WriteExpression(expression.Right);
        }
        private void WriteUnary(UnaryExpression expression)
        {
            var op = expression.Operator switch
            {
                SQLUnaryOperator.Positive => "+",
                SQLUnaryOperator.Negative => "-",
                SQLUnaryOperator.NOT => "NOT",

                _ => throw new NotSupportedException($"Unary with the operator {expression.Operator} is not Supported")
            };

            string space = expression.Operator == SQLUnaryOperator.NOT ? " " : "";
            _query.Append(op).Append(space);

            WriteExpression(expression.Operand);
        }
        private void WriteFunction(FunctionExpression expression)
        {
            _query.Append(expression.Name).Append("(");

            for (int i = 0; i < expression.Arguments.Count; i++)
            {
                if (i > 0)
                    _query.Append(", ");

                WriteExpression(expression.Arguments[i]);
            }

            _query.Append(")");
        }
        private void WriteCase(CaseExpression expression)
        {
            _query.Append("CASE ");

            if (expression.BaseExpression != null)
            {
                WriteExpression(expression.BaseExpression);
                _query.Append(' ');
            }

            _indent++;
            foreach (var c in expression.Conditions)
            {
                NewLine();
                IndentedLine();
                _query.Append("WHEN ");
                WriteExpression(c.Condition);

                _query.Append(" THEN ");
                WriteExpression(c.Result);
            }

            if (expression.ElseExpression != null)
            {
                NewLine();
                IndentedLine();
                _query.Append("ELSE ");
                WriteExpression(expression.ElseExpression);
            }
            _indent--;
            NewLine();
            IndentedLine();
            _query.Append("END");
        }
        private void WriteWildcard(WildcardExpression expression)
        {
            if (expression.TableAlias != null)
            {
                _query.Append($"{expression.TableAlias[0]}.");
            }

            _query.Append("*");
        }
        private void WriteIsNull(IsNullExpression expression)
        {
            WriteExpression(expression.Operand);
            _query.Append(" IS");

            if (expression.IsNot)
                _query.Append(" NOT");

            _query.Append(" NULL");
        }
        private void WriteLike(LikeExpression expression)
        {
            WriteExpression(expression.Operand);

            if (expression.IsNot)
                _query.Append(" NOT");
            _query.Append(" LIKE ");

            WriteExpression(expression.Pattern);
        }
        private void WriteIn(InExpression expression)
        {
            WriteExpression(expression.Operand);
            if (expression.IsNot)
                _query.Append(" NOT");
            _query.Append(" IN (");

            if (expression.Values.Count > 1)
            {
                for (int i = 0; i < expression.Values.Count; i++)
                {
                    if (i > 0)
                        _query.Append(", ");

                    WriteExpression(expression.Values[i]);
                }

                _query.Append(")");
            }
            else
            {
                WriteExpression(expression.Values[0]);
                _query.Append(")");
            }
        }
        private void WriteBetween(BetweenExpression expression)
        {
            WriteExpression(expression.Operand);
            if (expression.IsNot)
                _query.Append(" NOT");
            _query.Append(" BETWEEN ");
            WriteExpression(expression.Lower);
            _query.Append(" AND ");
            WriteExpression(expression.Upper);
        }
        private void WriteSelectExpression(SelectExpression expression)
        {
            WriteExpression(expression.Expression);
            if (expression.Alias != null)
                _query.Append($" AS {expression.Alias}");
        }
        private void WriteOrderByExpression(OrderByExpression expression)
        {
            WriteExpression(expression.Expression);
            if (expression.Descending)
                _query.Append(" DESC");
            else
                _query.Append(" ASC");
        }
        private void WriteParenthesized(ParenthesizedExpression expression)
        {
            _query.Append("(");
            WriteExpression(expression.Expression);
            _query.Append(")");
        }
        #endregion
        #region Helper
        private void NewLine()
        {
            _query.AppendLine();
        }
        private void IndentedLine()
        {
            _query.Append('\t', _indent);
        }
        #endregion
        // Currently Unsupported
        //private void WriteParameter(ParameterExpression expression)
        //{
        //    // TODO
        //}
    }
}
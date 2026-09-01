using SQLQueryBuilder.Helper;
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
        private SQLQueryResult _result = new();
        #region SQL Builder
        private void BuildSelect(SQLQuery sqlQuery)
        {
            _query.Append("SELECT");
            _indent++;

            if (sqlQuery.Select == null)
                throw new NullReferenceException("Select cannot be null.");
            if (sqlQuery.Select.Value.Count == 0)
                throw new InvalidOperationException("Select cannot be empty.");

            for (int i = 0; i < sqlQuery.Select.Value.Count; i++)
            {
                if (i > 0)
                    _query.Append(",");
                NewLine();
                IndentedLine();
                WriteExpression(sqlQuery.Select.Value[i]);
            }

            _indent--;
        }
        private void BuildFrom(SQLQuery sqlQuery)
        {
            if (sqlQuery.From == null)
                throw new NullReferenceException("From cannot be null.");

            NewLine();
            var names = sqlQuery.From.Value.TableName;
            _query.Append("FROM ");
            for (int i = 0; i < names.Count; i++)
            {
                AppendColumn(names[i]);
                if (i != names.Count - 1)
                    _query.Append(".");
            }
            if (sqlQuery.From.Value.Alias != null)
            {
                _query.Append($" AS ");
                AppendColumn(sqlQuery.From.Value.Alias);
            }
                
        }
        private void BuildJoins(SQLQuery sqlQuery)
        {
            if (sqlQuery.Joins.Count == 0) return;

            foreach (var join in sqlQuery.Joins)
            {
                string joinType = join.Value.JoinType switch
                {
                    SQLJoinType.INNER => $"INNER JOIN ",
                    SQLJoinType.LEFT => $"LEFT JOIN ",
                    SQLJoinType.RIGHT => $"RIGHT JOIN ",
                    SQLJoinType.FULL => $"FULL JOIN ",
                    _ => $"INNER JOIN "
                };

                NewLine();
                _query.Append(joinType);
                
                var joinTable = join.Value.Table.TableName;
                for (int i = 0; i < joinTable.Count; i++)
                {
                    AppendColumn(joinTable[i]);
                    if (i != joinTable.Count -1) 
                        _query.Append(".");
                }

                if (join.Value.Table.Alias != null)
                {
                    _query.Append(" AS ");
                    AppendColumn(join.Value.Table.Alias);
                }
                    

                _query.Append(" ON ");
                WriteExpression(join.Value.Condition, true);
            }
        }
        private void BuildHaving(SQLQuery sqlQuery)
        {
            if (sqlQuery.Having == null) return;

            NewLine();
            _query.Append("HAVING ");
            WriteExpression(sqlQuery.Having.Value, true);
        }
        private void BuildWhere(SQLQuery sqlQuery)
        {
            if (sqlQuery.Where == null) return;

            NewLine();
            _query.Append("WHERE ");
            WriteExpression(sqlQuery.Where.Value, true);
        }
        private void BuildGroupBy(SQLQuery sqlQuery)
        {
            if (sqlQuery.GroupBy == null) return;

            NewLine();
            _query.Append("GROUP BY ");
            for (int i = 0; i < sqlQuery.GroupBy.Value.Count; i++)
            {
                if (i > 0)
                    _query.Append(", ");

                WriteExpression(sqlQuery.GroupBy.Value[i]);
            }
        }
        private void BuildOrderBy(SQLQuery sqlQuery)
        {
            if (sqlQuery.OrderBy == null) return;

            NewLine();
            _query.Append("ORDER BY ");
            for (int i = 0; i < sqlQuery.OrderBy.Value.Count; i++)
            {
                if (i > 0)
                    _query.Append(", ");

                WriteExpression(sqlQuery.OrderBy.Value[i]);
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

        public SQLQueryResult Build()
        {
            _result.SetSQL(_query.ToString());
            return _result;
        }
        #endregion
        #region SQL Expression Writer
        private void WriteExpression(SQLExpression expression, bool parameterize = false)
        {
            switch (expression)
            {
                case ColumnExpression column:
                    WriteColumn(column, parameterize);
                    break;

                case LiteralExpression literal:
                    WriteLiteral(literal, parameterize);
                    break;

                case BinaryExpression binary:
                    WriteBinary(binary, parameterize);
                    break;

                case UnaryExpression unary:
                    WriteUnary(unary, parameterize);
                    break;

                case FunctionExpression function:
                    WriteFunction(function, parameterize);
                    break;

                case CaseExpression @case:
                    WriteCase(@case, parameterize);
                    break;

                case WildcardExpression wildcard:
                    WriteWildcard(wildcard, parameterize);
                    break;

                case IsNullExpression isNull:
                    WriteIsNull(isNull, parameterize);
                    break;

                case LikeExpression like:
                    WriteLike(like, parameterize);
                    break;

                case InExpression @in:
                    WriteIn(@in, parameterize);
                    break;

                case BetweenExpression between:
                    WriteBetween(between, parameterize);
                    break;

                case SelectExpression select:
                    WriteSelectExpression(select, parameterize);
                    break;

                case OrderByExpression orderBy:
                    WriteOrderByExpression(orderBy, parameterize);
                    break;

                case ParenthesizedExpression parenthesized:
                    WriteParenthesized(parenthesized, parameterize);
                    break;

                default:
                    throw new NotSupportedException(
                        $"SQL expression type '{expression.GetType().Name}' is not supported.");
            }
        }
        private void WriteLiteral(LiteralExpression expression, bool parameterize)
        {
            if (TryWriteParameter(expression.Value, parameterize))
                return;

            switch (expression.Type)
            {
                case SQLLiteralType.Money:
                    _query.Append('$').Append(expression.Value);
                    break;

                case SQLLiteralType.BinaryLiteral:
                    _query.Append("0x").Append(expression.Value);
                    break;

                case SQLLiteralType.UnicodeString:
                    _query.Append("N")
                          .Append(SQLParserHelper.EscapeStringLiteral((string)expression.Value));
                    break;

                case SQLLiteralType.String:
                    _query.Append(
                        SQLParserHelper.EscapeStringLiteral((string)expression.Value));
                    break;

                case SQLLiteralType.Scientific:
                case SQLLiteralType.Decimal:
                case SQLLiteralType.Integer:
                    _query.Append(expression.Value);
                    break;

                case SQLLiteralType.Date:
                    _query.Append("CAST('")
                          .Append(((DateOnly)expression.Value).ToString("yyyy-MM-dd"))
                          .Append("' AS DATE)");
                    break;

                case SQLLiteralType.Time:
                    _query.Append("CAST('")
                          .Append(((TimeOnly)expression.Value).ToString("HH:mm:ss.FFFFFFF"))
                          .Append("' AS TIME)");
                    break;

                case SQLLiteralType.Datetime:
                    _query.Append("CAST('")
                          .Append(((DateTime)expression.Value).ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF"))
                          .Append("' AS DATETIME)");
                    break;

                case SQLLiteralType.Null:
                    _query.Append("NULL");
                    break;
            }
        }
        private void WriteColumn(ColumnExpression expression, bool parameterize)
        {
            if(expression.Table != null)
            {
                foreach (var iden in expression.Table)
                    AppendColumn(iden);
            }
            _query.Append(".");
            AppendColumn(expression.Column);

        }
        private void WriteBinary(BinaryExpression expression, bool parameterize)
        {
            WriteExpression(expression.Left, parameterize);
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

            WriteExpression(expression.Right, parameterize);
        }
        private void WriteUnary(UnaryExpression expression, bool parameterize)
        {
            if (parameterize &&
            expression.Operand is LiteralExpression literal &&
            expression.Operator is SQLUnaryOperator.Positive or SQLUnaryOperator.Negative)
            {
                var value = literal.Value;

                if (expression.Operator == SQLUnaryOperator.Negative)
                {
                    value = Negate(value);
                }

                TryWriteParameter(value, true);
                return;
            }


            var op = expression.Operator switch
            {
                SQLUnaryOperator.Positive => "+",
                SQLUnaryOperator.Negative => "-",
                SQLUnaryOperator.NOT => "NOT",

                _ => throw new NotSupportedException($"Unary with the operator {expression.Operator} is not Supported")
            };

            string space = expression.Operator == SQLUnaryOperator.NOT ? " " : "";
            _query.Append(op).Append(space);

            WriteExpression(expression.Operand, parameterize);
        }
        private void WriteFunction(FunctionExpression expression, bool parameterize)
        {
            _query.Append(expression.Name).Append("(");

            for (int i = 0; i < expression.Arguments.Count; i++)
            {
                if (i > 0)
                    _query.Append(", ");

                WriteExpression(expression.Arguments[i], parameterize);
            }

            _query.Append(")");
        }
        private void WriteCase(CaseExpression expression, bool parameterize)
        {
            _query.Append("CASE ");

            if (expression.BaseExpression != null)
            {
                WriteExpression(expression.BaseExpression, parameterize);
                _query.Append(' ');
            }

            _indent++;
            foreach (var c in expression.Conditions)
            {
                NewLine();
                IndentedLine();
                _query.Append("WHEN ");
                WriteExpression(c.Condition, parameterize);

                _query.Append(" THEN ");
                WriteExpression(c.Result, parameterize);
            }

            if (expression.ElseExpression != null)
            {
                NewLine();
                IndentedLine();
                _query.Append("ELSE ");
                WriteExpression(expression.ElseExpression, parameterize);
            }
            _indent--;
            NewLine();
            IndentedLine();
            _query.Append("END");
        }
        private void WriteWildcard(WildcardExpression expression, bool parameterize)
        {
            if (expression.TableAlias != null)
            {
                for (var i = 0; i < expression.TableAlias.Count; i++)
                {
                    AppendColumn(expression.TableAlias[i]);
                    _query.Append(".");
                }
            }
            _query.Append("*");
        }
        private void WriteIsNull(IsNullExpression expression, bool parameterize)
        {
            WriteExpression(expression.Operand, parameterize);
            _query.Append(" IS");

            if (expression.IsNot)
                _query.Append(" NOT");

            _query.Append(" NULL");
        }
        private void WriteLike(LikeExpression expression, bool parameterize)
        {
            WriteExpression(expression.Operand, parameterize);

            if (expression.IsNot)
                _query.Append(" NOT");
            _query.Append(" LIKE ");

            WriteExpression(expression.Pattern, parameterize);
        }
        private void WriteIn(InExpression expression, bool parameterize)
        {
            WriteExpression(expression.Operand, parameterize);
            if (expression.IsNot)
                _query.Append(" NOT");
            _query.Append(" IN (");

            if (expression.Values.Count > 1)
            {
                for (int i = 0; i < expression.Values.Count; i++)
                {
                    if (i > 0)
                        _query.Append(", ");

                    WriteExpression(expression.Values[i], parameterize);
                }

                _query.Append(")");
            }
            else
            {
                WriteExpression(expression.Values[0], parameterize);
                _query.Append(")");
            }
        }
        private void WriteBetween(BetweenExpression expression, bool parameterize)
        {
            WriteExpression(expression.Operand, parameterize);
            if (expression.IsNot)
                _query.Append(" NOT");
            _query.Append(" BETWEEN ");
            WriteExpression(expression.Lower, parameterize);
            _query.Append(" AND ");
            WriteExpression(expression.Upper, parameterize);
        }
        private void WriteSelectExpression(SelectExpression expression, bool parameterize)
        {
            WriteExpression(expression.Expression, parameterize);
            if (expression.Alias != null)
                _query.Append($" AS {SQLParserHelper.EscapeIdentifierQuote(expression.Alias)}");
        }
        private void WriteOrderByExpression(OrderByExpression expression, bool parameterize)
        {
            WriteExpression(expression.Expression, parameterize);
            if (expression.Descending)
                _query.Append(" DESC");
            else
                _query.Append(" ASC");
        }
        private void WriteParenthesized(ParenthesizedExpression expression, bool parameterize)
        {
            _query.Append("(");
            WriteExpression(expression.Expression, parameterize);
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
        private bool TryWriteParameter(object value, bool parameterize)
        {
            if (!parameterize)
                return false;

            string paramName = $"@p{_result.NextParameterIndex()}";

            _query.Append(paramName);
            _result.AddParameter(paramName, value);

            return true;
        }
        private static object Negate(object value)
        {
            return value switch
            {
                int v => -v,
                long v => -v,
                float v => -v,
                double v => -v,
                decimal v => -v,

                _ => throw new InvalidOperationException(
                    $"Cannot negate value of type {value.GetType().Name}.")
            };
        }
        private void AppendColumn(SQLIdentifier identifier)
        {
            switch (identifier.Delimiter)
            {
                case SQLIdentifierDelimiter.Bracket:
                    _query.Append(SQLParserHelper.EscapeIdentifierBracket(identifier.Value));
                    break;
                case SQLIdentifierDelimiter.DoubleQuote:
                    _query.Append(SQLParserHelper.EscapeIdentifierQuote(identifier.Value));
                    break;
                default:
                    _query.Append(SQLParserHelper.EscapeIdentifierQuote(identifier.Value));
                    break;
            }
        }
        #endregion
        // Currently Unsupported
        //private void WriteParameter(ParameterExpression expression)
        //{
        //    // TODO
        //}
    }
}
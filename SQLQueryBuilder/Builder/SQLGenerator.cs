using Dapper;
using SQLQueryBuilder.Helper;
using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.QueryBuilder;
using SQLQueryBuilder.Model.SQL_Expression;
using SQLQueryBuilder.SQLEnum.QueryBuilder;
using SQLQueryBuilder.SQLEnum.SQLExpression;
using System.Text;

namespace SQLQueryBuilder.Builder
{
    internal class SQLGenerator
    {
        private readonly StringBuilder _temporaryQuery = new();
        private string _baseQuery = string.Empty;
        private DynamicParameters _temporaryParameters = new();
        private DynamicParameters _baseParameters = new();

        private SQLQueryResult _result = new();

        private int _baseParameterIndex = 1;
        private int _temporaryParameterIndex = 1;
        private int _indent = 0;

        #region SQL Builder
        private void BuildSelect(SQLQuery sqlQuery)
        {
            _temporaryQuery.Append("SELECT");
            _indent++;

            if (sqlQuery.Select == null)
                throw new NullReferenceException("Select cannot be null.");
            if (sqlQuery.Select.Value.Count == 0)
                throw new InvalidOperationException("Select cannot be empty.");

            for (int i = 0; i < sqlQuery.Select.Value.Count; i++)
            {
                if (i > 0)
                {
                    _temporaryQuery.Append(",");
                    NewLine();
                }

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
            _temporaryQuery.Append("FROM ");
            for (int i = 0; i < names.Count; i++)
            {
                AppendColumn(names[i]);
                if (i != names.Count - 1)
                    _temporaryQuery.Append(".");
            }
            if (sqlQuery.From.Value.Alias != null)
            {
                _temporaryQuery.Append($" AS ");
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
                _temporaryQuery.Append(joinType);
                
                var joinTable = join.Value.Table.TableName;
                for (int i = 0; i < joinTable.Count; i++)
                {
                    AppendColumn(joinTable[i]);
                    if (i != joinTable.Count -1) 
                        _temporaryQuery.Append(".");
                }

                if (join.Value.Table.Alias != null)
                {
                    _temporaryQuery.Append(" AS ");
                    AppendColumn(join.Value.Table.Alias);
                }
                    

                _temporaryQuery.Append(" ON ");
                WriteExpression(join.Value.Condition, true);
            }
        }
        private void BuildHaving(SQLQuery sqlQuery)
        {
            if (sqlQuery.Having == null) return;

            NewLine();
            _temporaryQuery.Append("HAVING ");
            WriteExpression(sqlQuery.Having.Value, true);
        }
        private void BuildWhere(SQLQuery sqlQuery)
        {
            if (sqlQuery.Where == null) return;

            NewLine();
            _temporaryQuery.Append("WHERE ");
            WriteExpression(sqlQuery.Where.Value, true);
        }
        private void BuildGroupBy(SQLQuery sqlQuery)
        {
            if (sqlQuery.GroupBy == null) return;

            NewLine();
            _temporaryQuery.Append("GROUP BY ");
            for (int i = 0; i < sqlQuery.GroupBy.Value.Count; i++)
            {
                if (i > 0)
                    _temporaryQuery.Append(", ");

                WriteExpression(sqlQuery.GroupBy.Value[i]);
            }
        }
        private void BuildOrderBy(SQLQuery sqlQuery)
        {
            if (sqlQuery.OrderBy == null) return;

            NewLine();
            _temporaryQuery.Append("ORDER BY ");
            for (int i = 0; i < sqlQuery.OrderBy.Value.Count; i++)
            {
                if (i > 0)
                    _temporaryQuery.Append(", ");

                WriteExpression(sqlQuery.OrderBy.Value[i]);
            }
        }
        private void BuildLimit(SQLQuery sqlQuery)
        {
            if(sqlQuery.Limit == null || sqlQuery.Limit < 0) return;

            NewLine();
            _temporaryQuery.Append($"LIMIT {sqlQuery.Limit}");
        }

        public void BuildBaseQuery(SQLQuery sqlQuery)
        {
            BuildSelect(sqlQuery);
            BuildFrom(sqlQuery);
            BuildJoins(sqlQuery);

            _baseQuery = _temporaryQuery.ToString();
            _baseParameterIndex = _temporaryParameterIndex;
            _baseParameters = _temporaryParameters;
        }
        public void BuildClauses(SQLQuery sqlQuery)
        {
            _temporaryQuery.Clear();
            _temporaryParameters = new DynamicParameters(_baseParameters);
            _temporaryParameterIndex = _baseParameterIndex;

            BuildWhere(sqlQuery);
            BuildGroupBy(sqlQuery);
            BuildHaving(sqlQuery);
            BuildOrderBy(sqlQuery);
            BuildLimit(sqlQuery);
        }

        public SQLQueryResult Build()
        {
            _result.SetSQL($"{_baseQuery}{_temporaryQuery.ToString()}");
            _result.SetParameterIndex(_temporaryParameterIndex);
            _result.SetParameter(_temporaryParameters);
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
                    _temporaryQuery.Append('$').Append(expression.Value);
                    break;

                case SQLLiteralType.BinaryLiteral:
                    _temporaryQuery.Append("0x").Append(expression.Value);
                    break;

                case SQLLiteralType.UnicodeString:
                    _temporaryQuery.Append("N")
                          .Append(SQLParserHelper.EscapeStringLiteral((string)expression.Value));
                    break;

                case SQLLiteralType.String:
                    _temporaryQuery.Append(
                        SQLParserHelper.EscapeStringLiteral((string)expression.Value));
                    break;

                case SQLLiteralType.Scientific:
                case SQLLiteralType.Decimal:
                case SQLLiteralType.Integer:
                    _temporaryQuery.Append(expression.Value);
                    break;

                case SQLLiteralType.Date:
                    _temporaryQuery.Append("CAST('")
                          .Append(((DateOnly)expression.Value).ToString("yyyy-MM-dd"))
                          .Append("' AS DATE)");
                    break;

                case SQLLiteralType.Time:
                    _temporaryQuery.Append("CAST('")
                          .Append(((TimeOnly)expression.Value).ToString("HH:mm:ss.FFFFFFF"))
                          .Append("' AS TIME)");
                    break;

                case SQLLiteralType.Datetime:
                    _temporaryQuery.Append("CAST('")
                          .Append(((DateTime)expression.Value).ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF"))
                          .Append("' AS DATETIME)");
                    break;

                case SQLLiteralType.Null:
                    _temporaryQuery.Append("NULL");
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
            _temporaryQuery.Append(".");
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

            _temporaryQuery.Append(' ').Append(op).Append(' ');

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
            _temporaryQuery.Append(op).Append(space);

            WriteExpression(expression.Operand, parameterize);
        }
        private void WriteFunction(FunctionExpression expression, bool parameterize)
        {
            _temporaryQuery.Append(expression.Name).Append("(");

            for (int i = 0; i < expression.Arguments.Count; i++)
            {
                if (i > 0)
                    _temporaryQuery.Append(", ");

                WriteExpression(expression.Arguments[i], parameterize);
            }

            _temporaryQuery.Append(")");
        }
        private void WriteCase(CaseExpression expression, bool parameterize)
        {
            _temporaryQuery.Append("CASE ");

            if (expression.BaseExpression != null)
            {
                WriteExpression(expression.BaseExpression, parameterize);
                _temporaryQuery.Append(' ');
            }

            _indent++;
            foreach (var c in expression.Conditions)
            {
                NewLine();
                IndentedLine();
                _temporaryQuery.Append("WHEN ");
                WriteExpression(c.Condition, parameterize);

                _temporaryQuery.Append(" THEN ");
                WriteExpression(c.Result, parameterize);
            }

            if (expression.ElseExpression != null)
            {
                NewLine();
                IndentedLine();
                _temporaryQuery.Append("ELSE ");
                WriteExpression(expression.ElseExpression, parameterize);
            }
            _indent--;
            NewLine();
            IndentedLine();
            _temporaryQuery.Append("END");
        }
        private void WriteWildcard(WildcardExpression expression, bool parameterize)
        {
            if (expression.TableAlias != null)
            {
                for (var i = 0; i < expression.TableAlias.Count; i++)
                {
                    AppendColumn(expression.TableAlias[i]);
                    _temporaryQuery.Append(".");
                }
            }
            _temporaryQuery.Append("*");
        }
        private void WriteIsNull(IsNullExpression expression, bool parameterize)
        {
            WriteExpression(expression.Operand, parameterize);
            _temporaryQuery.Append(" IS");

            if (expression.IsNot)
                _temporaryQuery.Append(" NOT");

            _temporaryQuery.Append(" NULL");
        }
        private void WriteLike(LikeExpression expression, bool parameterize)
        {
            WriteExpression(expression.Operand, parameterize);

            if (expression.IsNot)
                _temporaryQuery.Append(" NOT");
            _temporaryQuery.Append(" LIKE ");

            WriteExpression(expression.Pattern, parameterize);
        }
        private void WriteIn(InExpression expression, bool parameterize)
        {
            WriteExpression(expression.Operand, parameterize);
            if (expression.IsNot)
                _temporaryQuery.Append(" NOT");
            _temporaryQuery.Append(" IN (");

            if (expression.Values.Count > 1)
            {
                for (int i = 0; i < expression.Values.Count; i++)
                {
                    if (i > 0)
                        _temporaryQuery.Append(", ");

                    WriteExpression(expression.Values[i], parameterize);
                }

                _temporaryQuery.Append(")");
            }
            else
            {
                WriteExpression(expression.Values[0], parameterize);
                _temporaryQuery.Append(")");
            }
        }
        private void WriteBetween(BetweenExpression expression, bool parameterize)
        {
            WriteExpression(expression.Operand, parameterize);
            if (expression.IsNot)
                _temporaryQuery.Append(" NOT");
            _temporaryQuery.Append(" BETWEEN ");
            WriteExpression(expression.Lower, parameterize);
            _temporaryQuery.Append(" AND ");
            WriteExpression(expression.Upper, parameterize);
        }
        private void WriteSelectExpression(SelectExpression expression, bool parameterize)
        {
            WriteExpression(expression.Expression, parameterize);
            if (expression.Alias != null)
            {
                _temporaryQuery.Append($" AS ");
                AppendColumn(expression.Alias);
            }
                

        }
        private void WriteOrderByExpression(OrderByExpression expression, bool parameterize)
        {
            WriteExpression(expression.Expression, parameterize);
            if (expression.Descending)
                _temporaryQuery.Append(" DESC");
            else
                _temporaryQuery.Append(" ASC");
        }
        private void WriteParenthesized(ParenthesizedExpression expression, bool parameterize)
        {
            _temporaryQuery.Append("(");
            WriteExpression(expression.Expression, parameterize);
            _temporaryQuery.Append(")");
        }
        #endregion
        #region Helper
        private void NewLine()
        {
            _temporaryQuery.AppendLine();
        }
        private void IndentedLine()
        {
            _temporaryQuery.Append('\t', _indent);
        }
        private bool TryWriteParameter(object value, bool parameterize)
        {
            if (!parameterize)
                return false;

            string paramName = $"@p{_temporaryParameterIndex++}";

            _temporaryQuery.Append(paramName);
            _temporaryParameters.Add(paramName, value);

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
                    _temporaryQuery.Append(SQLParserHelper.EscapeIdentifierBracket(identifier.Value));
                    break;
                case SQLIdentifierDelimiter.DoubleQuote:
                    _temporaryQuery.Append(SQLParserHelper.EscapeIdentifierQuote(identifier.Value));
                    break;
                default:
                    _temporaryQuery.Append(SQLParserHelper.EscapeIdentifierQuote(identifier.Value));
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
using SQLQueryBuilder.Core.SQLEnum.SQLExpression;
using SQLQueryBuilder.Core.SQLExpression;
using SQLQueryBuilder.Helper;
using SQLQueryBuilder.Model.Context;
using SQLQueryBuilder.Model.QueryBuilder;
using SQLQueryBuilder.SQLEnum.QueryBuilder;

namespace SQLQueryBuilder.Builder
{
    internal static class SQLGenerator
    {
        #region SQL Builder
        public static void BuildSelect(List<SelectExpression> select, ref SQLGeneratorContext context)
        {
            Parameterize(ref context);

            context.TemporaryString.Append("SELECT");
            context.IncreaseIndent();

            if (select == null)
                throw new NullReferenceException("Select cannot be null.");
            if (select.Count == 0)
                throw new InvalidOperationException("Select cannot be empty.");

            for (int i = 0; i < select.Count; i++)
            {
                if (i > 0)
                {
                    context.TemporaryString.Append(",");
                    NewLine(ref context);
                }

                IndentedLine(ref context);
                writeExpression(ref context, select[i]);
            }

            context.DecreaseIndent();
        }
        public static void BuildFrom(TableSource from, ref SQLGeneratorContext context)
        {
            if (from == null)
                throw new NullReferenceException("From cannot be null.");

            Parameterize(ref context);

            NewLine(ref context);
            var names = from.TableName;
            context.TemporaryString.Append("FROM ");
            for (int i = 0; i < names.Count; i++)
            {
                AppendColumn(names[i], ref context);
                if (i != names.Count - 1)
                    context.TemporaryString.Append(".");
            }
            if (from.Alias != null)
            {
                context.TemporaryString.Append($" AS ");
                AppendColumn(from.Alias, ref context);
            }

        }
        public static void BuildJoins(List<JoinSource> joins, ref SQLGeneratorContext context)
        {
            if (joins == null) return;
            if (joins.Count == 0) return;

            foreach (var join in joins)
            {
                string joinType = join.JoinType switch
                {
                    SQLJoinType.INNER => $"INNER JOIN ",
                    SQLJoinType.LEFT => $"LEFT JOIN ",
                    SQLJoinType.RIGHT => $"RIGHT JOIN ",
                    SQLJoinType.FULL => $"FULL JOIN ",
                    _ => $"INNER JOIN "
                };

                NewLine(ref context);
                context.TemporaryString.Append(joinType);

                var joinTable = join.Table.TableName;
                for (int i = 0; i < joinTable.Count; i++)
                {
                    AppendColumn(joinTable[i], ref context);
                    if (i != joinTable.Count - 1)
                        context.TemporaryString.Append(".");
                }

                if (join.Table.Alias != null)
                {
                    context.TemporaryString.Append(" AS ");
                    AppendColumn(join.Table.Alias, ref context);
                }

                context.TemporaryString.Append(" ON ");

                Parameterize(ref context, true);
                writeExpression(ref context, join.Condition);
            }
        }
        public static void BuildHaving(SQLExpression having, ref SQLGeneratorContext context)
        {
            if (having == null) return;

            NewLine(ref context);
            context.TemporaryString.Append("HAVING ");

            Parameterize(ref context, true);
            writeExpression(ref context, having);
        }
        public static void BuildWhere(SQLExpression where, ref SQLGeneratorContext context)
        {
            if (where == null) return;

            NewLine(ref context);
            context.TemporaryString.Append("WHERE ");

            Parameterize(ref context, true);
            writeExpression(ref context, where);
        }
        public static void BuildGroupBy(List<SQLExpression> groupBy, ref SQLGeneratorContext context)
        {
            if (groupBy == null) return;
            if (groupBy.Count == 0) return;

            NewLine(ref context);
            context.TemporaryString.Append("GROUP BY ");
            for (int i = 0; i < groupBy.Count; i++)
            {
                if (i > 0)
                    context.TemporaryString.Append(", ");

                Parameterize(ref context, true);
                writeExpression(ref context, groupBy[i]);
            }
        }
        public static void BuildOrderBy(List<OrderByExpression> orderBy, ref SQLGeneratorContext context)
        {
            if (orderBy == null) return;
            if (orderBy.Count == 0) return;

            NewLine(ref context);
            context.TemporaryString.Append("ORDER BY ");
            for (int i = 0; i < orderBy.Count; i++)
            {
                if (i > 0)
                    context.TemporaryString.Append(", ");

                Parameterize(ref context, true);
                writeExpression(ref context, orderBy[i]);
            }
        }
        public static void BuildLimit(int? limit, ref SQLGeneratorContext context)
        {
            if (limit == null || limit < 0) return;

            NewLine(ref context);
            context.TemporaryString.Append($"LIMIT {limit}");
        }

        public static SQLQueryResult Build(ref SQLGeneratorContext context)
        {
            var result = new SQLQueryResult();
            result.SetSQL(context.TemporaryString.ToString());
            result.SetParameterIndex(context.TemporaryIndex);
            result.SetParameter(context.TemporaryParameters);
            return result;
        }
        #endregion

        #region SQL Expression Writer
        private static void writeExpression(ref SQLGeneratorContext context, SQLExpression expression)
        {
            switch (expression)
            {
                case ColumnExpression column:
                    WriteColumn(column, ref context);
                    break;

                case LiteralExpression literal:
                    WriteLiteral(literal, ref context);
                    break;

                case BinaryExpression binary:
                    WriteBinary(binary, ref context);
                    break;

                case UnaryExpression unary:
                    WriteUnary(unary, ref context);
                    break;

                case FunctionExpression function:
                    WriteFunction(function, ref context);
                    break;

                case CaseExpression @case:
                    WriteCase(@case, ref context);
                    break;

                case WildcardExpression wildcard:
                    WriteWildcard(wildcard, ref context);
                    break;

                case IsNullExpression isNull:
                    WriteIsNull(isNull, ref context);
                    break;

                case LikeExpression like:
                    WriteLike(like, ref context);
                    break;

                case InExpression @in:
                    WriteIn(@in, ref context);
                    break;

                case BetweenExpression between:
                    WriteBetween(between, ref context);
                    break;

                case SelectExpression select:
                    WriteSelectExpression(select, ref context);
                    break;

                case OrderByExpression orderBy:
                    WriteOrderByExpression(orderBy, ref context);
                    break;

                case ParenthesizedExpression parenthesized:
                    WriteParenthesized(parenthesized, ref context);
                    break;

                default:
                    throw new NotSupportedException(
                        $"SQL expression type '{expression.GetType().Name}' is not supported.");
            }
        }
        private static void WriteLiteral(LiteralExpression expression, ref SQLGeneratorContext context)
        {
            if (TryWriteParameter(expression.Value, ref context))
                return;

            switch (expression.Type)
            {
                case SQLLiteralType.Money:
                    context.TemporaryString.Append('$').Append(expression.Value);
                    break;

                case SQLLiteralType.BinaryLiteral:
                    context.TemporaryString.Append("0x").Append(expression.Value);
                    break;

                case SQLLiteralType.UnicodeString:
                    context.TemporaryString.Append("N")
                          .Append(SQLParserHelper.EscapeStringLiteral((string)expression.Value));
                    break;

                case SQLLiteralType.String:
                    context.TemporaryString.Append(
                        SQLParserHelper.EscapeStringLiteral((string)expression.Value));
                    break;

                case SQLLiteralType.Scientific:
                case SQLLiteralType.Decimal:
                case SQLLiteralType.Integer:
                    context.TemporaryString.Append(expression.Value);
                    break;

                case SQLLiteralType.Date:
                    context.TemporaryString.Append("CAST('")
                          .Append(((DateOnly)expression.Value).ToString("yyyy-MM-dd"))
                          .Append("' AS DATE)");
                    break;

                case SQLLiteralType.Time:
                    context.TemporaryString.Append("CAST('")
                          .Append(((TimeOnly)expression.Value).ToString("HH:mm:ss.FFFFFFF"))
                          .Append("' AS TIME)");
                    break;

                case SQLLiteralType.Datetime:
                    context.TemporaryString.Append("CAST('")
                          .Append(((DateTime)expression.Value).ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF"))
                          .Append("' AS DATETIME)");
                    break;

                case SQLLiteralType.Null:
                    context.TemporaryString.Append("NULL");
                    break;
            }
        }
        private static void WriteColumn(ColumnExpression expression, ref SQLGeneratorContext context)
        {
            if (expression.Table != null)
            {
                foreach (var iden in expression.Table)
                    AppendColumn(iden, ref context);
                context.TemporaryString.Append(".");
            }
            
            AppendColumn(expression.Column, ref context);

        }
        private static void WriteBinary(BinaryExpression expression, ref SQLGeneratorContext context)
        {
            writeExpression(ref context, expression.Left);
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

            context.TemporaryString.Append(' ').Append(op).Append(' ');

            writeExpression(ref context, expression.Right);
        }
        private static void WriteUnary(UnaryExpression expression, ref SQLGeneratorContext context)
        {
            if (context.Parameterize &&
            expression.Operand is LiteralExpression literal &&
            expression.Operator is SQLUnaryOperator.Positive or SQLUnaryOperator.Negative)
            {
                var value = literal.Value;

                if (expression.Operator == SQLUnaryOperator.Negative)
                {
                    value = Negate(value);
                }

                TryWriteParameter(value, ref context);
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
            context.TemporaryString.Append(op).Append(space);

            writeExpression(ref context, expression.Operand);
        }
        private static void WriteFunction(FunctionExpression expression, ref SQLGeneratorContext context)
        {
            context.TemporaryString.Append(expression.Name).Append("(");

            for (int i = 0; i < expression.Arguments.Count; i++)
            {
                if (i > 0)
                    context.TemporaryString.Append(", ");

                writeExpression(ref context, expression.Arguments[i]);
            }

            context.TemporaryString.Append(")");
        }
        private static void WriteCase(CaseExpression expression, ref SQLGeneratorContext context)
        {
            context.TemporaryString.Append("CASE ");

            if (expression.BaseExpression != null)
            {
                writeExpression(ref context, expression.BaseExpression);
                context.TemporaryString.Append(' ');
            }

            context.IncreaseIndent();
            foreach (var c in expression.Conditions)
            {
                NewLine(ref context);
                IndentedLine(ref context);
                context.TemporaryString.Append("WHEN ");
                writeExpression(ref context, c.Condition);

                context.TemporaryString.Append(" THEN ");
                writeExpression(ref context, c.Result);
            }

            if (expression.ElseExpression != null)
            {
                NewLine(ref context);
                IndentedLine(ref context);
                context.TemporaryString.Append("ELSE ");
                writeExpression(ref context, expression.ElseExpression);
            }
            context.IncreaseIndent();
            NewLine(ref context);
            IndentedLine(ref context);
            context.TemporaryString.Append("END");
        }
        private static void WriteWildcard(WildcardExpression expression, ref SQLGeneratorContext context)
        {
            if (expression.TableAlias != null)
            {
                for (var i = 0; i < expression.TableAlias.Count; i++)
                {
                    AppendColumn(expression.TableAlias[i], ref context);
                    context.TemporaryString.Append(".");
                }
            }
            context.TemporaryString.Append("*");
        }
        private static void WriteIsNull(IsNullExpression expression, ref SQLGeneratorContext context)
        {
            writeExpression(ref context, expression.Operand);
            context.TemporaryString.Append(" IS");

            if (expression.IsNot)
                context.TemporaryString.Append(" NOT");

            context.TemporaryString.Append(" NULL");
        }
        private static void WriteLike(LikeExpression expression, ref SQLGeneratorContext context)
        {
            writeExpression(ref context, expression.Operand);

            if (expression.IsNot)
                context.TemporaryString.Append(" NOT");
            context.TemporaryString.Append(" LIKE ");

            writeExpression(ref context, expression.Pattern);
        }
        private static void WriteIn(InExpression expression, ref SQLGeneratorContext context)
        {
            writeExpression(ref context, expression.Operand);
            if (expression.IsNot)
                context.TemporaryString.Append(" NOT");
            context.TemporaryString.Append(" IN (");

            if (expression.Values.Count > 1)
            {
                for (int i = 0; i < expression.Values.Count; i++)
                {
                    if (i > 0)
                        context.TemporaryString.Append(", ");

                    writeExpression(ref context, expression.Values[i]);
                }

                context.TemporaryString.Append(")");
            }
            else
            {
                writeExpression(ref context, expression.Values[0]);
                context.TemporaryString.Append(")");
            }
        }
        private static void WriteBetween(BetweenExpression expression, ref SQLGeneratorContext context)
        {
            writeExpression(ref context, expression.Operand);
            if (expression.IsNot)
                context.TemporaryString.Append(" NOT");
            context.TemporaryString.Append(" BETWEEN ");
            writeExpression(ref context, expression.Lower);
            context.TemporaryString.Append(" AND ");
            writeExpression(ref context, expression.Upper);
        }
        private static void WriteSelectExpression(SelectExpression expression, ref SQLGeneratorContext context)
        {
            writeExpression(ref context, expression.Expression);
            if (expression.Alias != null)
            {
                context.TemporaryString.Append($" AS ");
                AppendColumn(expression.Alias, ref context);
            }
        }
        private static void WriteOrderByExpression(OrderByExpression expression, ref SQLGeneratorContext context)
        {
            writeExpression(ref context, expression.Expression);
            if (expression.Descending)
                context.TemporaryString.Append(" DESC");
            else
                context.TemporaryString.Append(" ASC");
        }
        private static void WriteParenthesized(ParenthesizedExpression expression, ref SQLGeneratorContext context)
        {
            context.TemporaryString.Append("(");
            writeExpression(ref context, expression.Expression);
            context.TemporaryString.Append(")");
        }
        #endregion

        #region Helper
        private static void NewLine(ref SQLGeneratorContext context)
        {
            context.TemporaryString.AppendLine();
        }
        private static void IndentedLine(ref SQLGeneratorContext context)
        {
            context.TemporaryString.Append('\t', context.Indent);
        }
        private static bool TryWriteParameter(object value, ref SQLGeneratorContext context)
        {
            if (!context.Parameterize)
                return false;

            string paramName = $"@p{context.GetNextIndex()}";

            context.TemporaryString.Append(paramName);
            context.TemporaryParameters.Add(paramName, value);

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
        private static void AppendColumn(SQLIdentifier identifier, ref SQLGeneratorContext context)
        {
            switch (identifier.Delimiter)
            {
                case SQLIdentifierDelimiter.Bracket:
                    context.TemporaryString.Append(SQLParserHelper.EscapeIdentifierBracket(identifier.Value));
                    break;
                case SQLIdentifierDelimiter.DoubleQuote:
                    context.TemporaryString.Append(SQLParserHelper.EscapeIdentifierQuote(identifier.Value));
                    break;
                default:
                    context.TemporaryString.Append(SQLParserHelper.EscapeIdentifierQuote(identifier.Value));
                    break;
            }
        }
        private static void Parameterize(ref SQLGeneratorContext context, bool parameterize = false)
        {
            context.SetParameterize(parameterize);
        }
        #endregion
        // Currently Unsupported
        //private static void WriteParameter(ParameterExpression expression)
        //{
        //    // TODO
        //}
    }
}
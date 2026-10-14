using SQLQueryBuilder.Core.QueryBuilder;
using SQLQueryBuilder.Core.SQLEnum.QueryBuilder;
using SQLQueryBuilder.Core.SQLEnum.SQLExpression;
using SQLQueryBuilder.Core.SQLExpression;
using SQLQueryBuilder.Helper;
using SQLQueryBuilder.Model.Context;
using System.Collections.Frozen;
using System.Data;

namespace SQLQueryBuilder.Builder
{
    internal static class SQLGenerator
    {
        #region SQL Builder
        public static void BuildSelect(List<SelectExpression> select, ref SQLGeneratorContext context)
        {
            Parameterize(ref context);

            WriteValue("SELECT", ref context);
            context.IncreaseIndent();

            if (select == null)
                throw new NullReferenceException("Select cannot be null.");
            if (select.Count == 0)
                throw new InvalidOperationException("Select cannot be empty.");

            for (int i = 0; i < select.Count; i++)
            {
                if (i > 0)
                {
                    WriteValue(",", ref context);
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
            WriteValue("FROM ", ref context);
            for (int i = 0; i < names.Count; i++)
            {
                AppendColumn(names[i], ref context);
                if (i != names.Count - 1)
                    WriteValue(".", ref context);
            }
            if (from.Alias != null)
            {
                WriteValue($" AS ", ref context);
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
                WriteValue(joinType, ref context);

                var joinTable = join.Table.TableName;
                for (int i = 0; i < joinTable.Count; i++)
                {
                    AppendColumn(joinTable[i], ref context);
                    if (i != joinTable.Count - 1)
                        WriteValue(".", ref context);
                }

                if (join.Table.Alias != null)
                {
                    WriteValue(" AS ", ref context);
                    AppendColumn(join.Table.Alias, ref context);
                }

                WriteValue(" ON ", ref context);

                Parameterize(ref context, true);
                writeExpression(ref context, join.Condition);
            }
        }
        public static void BuildHaving(SQLExpression having, ref SQLGeneratorContext context)
        {
            if (having == null) return;

            NewLine(ref context);
            WriteValue("HAVING ", ref context);

            Parameterize(ref context, true);
            writeExpression(ref context, having);
        }
        public static void BuildWhere(SQLExpression where, ref SQLGeneratorContext context)
        {
            if (where == null) return;

            NewLine(ref context);
            WriteValue("WHERE ", ref context);

            Parameterize(ref context, true);
            writeExpression(ref context, where);
        }
        public static void BuildGroupBy(List<SQLExpression> groupBy, ref SQLGeneratorContext context)
        {
            if (groupBy == null) return;
            if (groupBy.Count == 0) return;

            NewLine(ref context);
            WriteValue("GROUP BY ", ref context);
            for (int i = 0; i < groupBy.Count; i++)
            {
                if (i > 0)
                    WriteValue(", ", ref context);

                Parameterize(ref context, true);
                writeExpression(ref context, groupBy[i]);
            }
        }
        public static void BuildOrderBy(List<OrderByExpression> orderBy, ref SQLGeneratorContext context)
        {
            if (orderBy == null) return;
            if (orderBy.Count == 0) return;

            NewLine(ref context);
            WriteValue("ORDER BY ", ref context);
            for (int i = 0; i < orderBy.Count; i++)
            {
                if (i > 0)
                    WriteValue(", ", ref context);

                Parameterize(ref context, true);
                writeExpression(ref context, orderBy[i]);
            }
        }
        public static void BuildLimit(int? limit, ref SQLGeneratorContext context)
        {
            if (limit == null || limit < 0) return;

            NewLine(ref context);
            WriteValue($"LIMIT {limit}", ref context);
        }

        public static SQLQueryResult Build(ref SQLGeneratorContext context)
        {
            var temp = new Dictionary<string, QueryParameter>(
                context.TempParameters.Count + (context.GetBase?.Parameters.Count ?? 0));

            foreach (var param in context.TempParameters)
                temp[param.Key] = param.Value;

            if (context.GetBase is not null)
            {
                foreach (var param in context.GetBase.Parameters)
                    temp[param.Key] = param.Value;
            }

            return new SQLQueryResult(
                temp.ToFrozenDictionary(),
                context.TempParametizedSQL.ToString(),
                context.TempRawSQL.ToString()
            );
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
            switch (expression.Type)
            {
                case SQLLiteralType.Money:
                    if(context.Parameterize)
                        WriteRaw("$", ref context);
                    else
                        WriteValue("$", ref context);

                    WriteValue(expression.Value, DbType.Decimal, ref context);
                    break;

                case SQLLiteralType.BinaryLiteral:
                    if (context.Parameterize)
                        WriteRaw("0x", ref context);
                    else
                        WriteValue("0x", ref context);

                    WriteValue(expression.Value, DbType.Binary, ref context);
                    break;

                case SQLLiteralType.UnicodeString:
                    if (context.Parameterize)
                        WriteRaw("N", ref context);
                    else
                        WriteValue("N", ref context);

                    WriteValue(expression.Value, DbType.String, ref context);
                    break;

                case SQLLiteralType.String:
                    WriteValue(expression.Value, DbType.String, ref context);
                    break;

                case SQLLiteralType.Scientific:
                    WriteValue(expression.Value, DbType.Double, ref context);
                    break;
                case SQLLiteralType.Decimal:
                    WriteValue(expression.Value, DbType.Decimal, ref context);
                    break;
                case SQLLiteralType.Integer:
                    WriteValue(expression.Value, DbType.Int32, ref context);
                    break;

                case SQLLiteralType.Date:
                    WriteValue("CAST('", ref context);
                    WriteValue(((DateOnly)expression.Value).ToString("yyyy-MM-dd"), DbType.Date, ref context);
                    WriteValue("' AS DATE)", ref context);
                    break;

                case SQLLiteralType.Time:
                    WriteValue("CAST('", ref context);
                    WriteValue(((TimeOnly)expression.Value).ToString("HH:mm:ss.FFFFFFF"), DbType.Time, ref context);
                    WriteValue("' AS TIME)", ref context);
                    break;

                case SQLLiteralType.Datetime:
                    WriteValue("CAST('", ref context);
                    WriteValue(((DateTime)expression.Value).ToString("yyyy-MM-dd HH:mm:ss.FFFFFFF"), DbType.DateTime, ref context);
                    WriteValue("' AS DATETIME)", ref context);
                    break;

                case SQLLiteralType.Null:
                    WriteValue("NULL", ref context);
                    break;
            }
        }
        private static void WriteColumn(ColumnExpression expression, ref SQLGeneratorContext context)
        {
            if (expression.Table != null)
            {
                foreach (var iden in expression.Table)
                    AppendColumn(iden, ref context);
                WriteValue(".", ref context);
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

            WriteValue(' ', ref context);
            WriteValue(op, ref context);
            WriteValue(' ', ref context);

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
                    Negate(value, ref context);
                }
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
            WriteValue(op, ref context);
            WriteValue(space, ref context);

            writeExpression(ref context, expression.Operand);
        }
        private static void WriteFunction(FunctionExpression expression, ref SQLGeneratorContext context)
        {
            context.TempParametizedSQL.Append(expression.Name).Append("(");

            for (int i = 0; i < expression.Arguments.Count; i++)
            {
                if (i > 0)
                    WriteValue(", ", ref context);

                writeExpression(ref context, expression.Arguments[i]);
            }

            WriteValue(")", ref context);
        }
        private static void WriteCase(CaseExpression expression, ref SQLGeneratorContext context)
        {
            WriteValue("CASE ", ref context);

            if (expression.BaseExpression != null)
            {
                writeExpression(ref context, expression.BaseExpression);
                context.TempParametizedSQL.Append(' ');
            }

            context.IncreaseIndent();
            foreach (var c in expression.Conditions)
            {
                NewLine(ref context);
                IndentedLine(ref context);
                WriteValue("WHEN ", ref context);
                writeExpression(ref context, c.Condition);

                WriteValue(" THEN ", ref context);
                writeExpression(ref context, c.Result);
            }

            if (expression.ElseExpression != null)
            {
                NewLine(ref context);
                IndentedLine(ref context);
                WriteValue("ELSE ", ref context);
                writeExpression(ref context, expression.ElseExpression);
            }
            context.IncreaseIndent();
            NewLine(ref context);
            IndentedLine(ref context);
            WriteValue("END", ref context);
        }
        private static void WriteWildcard(WildcardExpression expression, ref SQLGeneratorContext context)
        {
            if (expression.TableAlias != null)
            {
                for (var i = 0; i < expression.TableAlias.Count; i++)
                {
                    AppendColumn(expression.TableAlias[i], ref context);
                    WriteValue(".", ref context);
                }
            }
            WriteValue("*", ref context);
        }
        private static void WriteIsNull(IsNullExpression expression, ref SQLGeneratorContext context)
        {
            writeExpression(ref context, expression.Operand);
            WriteValue(" IS", ref context);

            if (expression.IsNot)
                WriteValue(" NOT", ref context);

            WriteValue(" NULL", ref context);
        }
        private static void WriteLike(LikeExpression expression, ref SQLGeneratorContext context)
        {
            writeExpression(ref context, expression.Operand);

            if (expression.IsNot)
                WriteValue(" NOT", ref context);
            WriteValue(" LIKE ", ref context);

            writeExpression(ref context, expression.Pattern);
        }
        private static void WriteIn(InExpression expression, ref SQLGeneratorContext context)
        {
            writeExpression(ref context, expression.Operand);
            if (expression.IsNot)
                WriteValue(" NOT", ref context);
            WriteValue(" IN (", ref context);

            if (expression.Values.Count > 1)
            {
                for (int i = 0; i < expression.Values.Count; i++)
                {
                    if (i > 0)
                        WriteValue(", ", ref context);

                    writeExpression(ref context, expression.Values[i]);
                }

                WriteValue(")", ref context);
            }
            else
            {
                writeExpression(ref context, expression.Values[0]);
                WriteValue(")", ref context);
            }
        }
        private static void WriteBetween(BetweenExpression expression, ref SQLGeneratorContext context)
        {
            writeExpression(ref context, expression.Operand);
            if (expression.IsNot)
                WriteValue(" NOT", ref context);
            WriteValue(" BETWEEN ", ref context);
            writeExpression(ref context, expression.Lower);
            WriteValue(" AND ", ref context);
            writeExpression(ref context, expression.Upper);
        }
        private static void WriteSelectExpression(SelectExpression expression, ref SQLGeneratorContext context)
        {
            writeExpression(ref context, expression.Expression);
            if (expression.Alias != null)
            {
                WriteValue($" AS ", ref context);
                AppendColumn(expression.Alias, ref context);
            }
        }
        private static void WriteOrderByExpression(OrderByExpression expression, ref SQLGeneratorContext context)
        {
            writeExpression(ref context, expression.Expression);
            if (expression.Descending)
                WriteValue(" DESC", ref context);
            else
                WriteValue(" ASC", ref context);
        }
        private static void WriteParenthesized(ParenthesizedExpression expression, ref SQLGeneratorContext context)
        {
            WriteValue("(", ref context);
            writeExpression(ref context, expression.Expression);
            WriteValue(")", ref context);
        }
        #endregion

        #region Helper
        private static void NewLine(ref SQLGeneratorContext context)
        {
            context.TempParametizedSQL.AppendLine();
            context.TempRawSQL.AppendLine();
        }
        private static void IndentedLine(ref SQLGeneratorContext context)
        {
            context.TempParametizedSQL.Append('\t', context.Indent);
            context.TempRawSQL.Append('\t', context.Indent);
        }
        private static void Negate(object value, ref SQLGeneratorContext context)
        {
            switch (value)
            {
                case int v:
                    WriteValue(-v, DbType.Int32, ref context);
                    break;

                case long v:
                    WriteValue(-v, DbType.Int64, ref context);
                    break;

                case float v:
                    WriteValue(-v, DbType.Single, ref context);
                    break;

                case double v:
                    WriteValue(-v, DbType.Double, ref context);
                    break;

                case decimal v:
                    WriteValue(-v, DbType.Decimal, ref context);
                    break;
            }
        }
        private static void AppendColumn(SQLIdentifier identifier, ref SQLGeneratorContext context)
        {
            switch (identifier.Delimiter)
            {
                case SQLIdentifierDelimiter.Bracket:
                    WriteValue(SQLParserHelper.EscapeIdentifierBracket(identifier.Value), ref context);
                    break;
                case SQLIdentifierDelimiter.DoubleQuote:
                default:
                    WriteValue(SQLParserHelper.EscapeIdentifierQuote(identifier.Value), ref context);
                    break;
            }
        }

        private static void Parameterize(ref SQLGeneratorContext context, bool parameterize = false)
        {
            context.SetParameterize(parameterize);
        }
        private static void WriteRaw(object value, ref SQLGeneratorContext context) => context.TempRawSQL.Append(value);
        private static void WriteValue(object value, ref SQLGeneratorContext context)
        {
            context.TempRawSQL.Append(value);
            context.TempParametizedSQL.Append(value);
        }
        private static void WriteValue(object value, DbType type, ref SQLGeneratorContext context)
        {
            var escaped = value;

            if (type == DbType.String)
            {
                escaped = SQLParserHelper.EscapeStringLiteral((string)value);
            }

            if (context.Parameterize)
            {
                string paramName = $"@p{context.GetNextIndex()}";

                context.TempParametizedSQL.Append(paramName);

                context.TempParameters[paramName] = new QueryParameter(value, type);
            }
            else
            {
                context.TempParametizedSQL.Append(escaped);
            }

            context.TempRawSQL.Append(escaped);
        }
        #endregion
        // Currently Unsupported
        //private static void WriteParameter(ParameterExpression expression)
        //{
        //    // TODO
        //}
    }
}
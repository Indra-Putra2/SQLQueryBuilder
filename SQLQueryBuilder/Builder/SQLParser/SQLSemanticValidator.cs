using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.SQL_Expression;
using SQLQueryBuilder.SQLEnum;
using System.Data;
using System.Data.Common;
using System.Diagnostics;

namespace SQLQueryBuilder.Builder.SQLParser
{
    public class SQLSemanticValidator
    {
        private SQLTableManager _tableManager;
        private SQLDTOManager _dtoManager;
        private SQLQueryContext context;

        public SQLSemanticValidator()
        {
            _tableManager = new SQLTableManager();
            _dtoManager = new SQLDTOManager();
        }

        #region Validation
        public void ValidateQuery(SQLQuery query)
        {
            ValidateBaseQuery(query);
            ValidateClauses(query);
        }

        public void ValidateBaseQuery(SQLQuery query)
        {
            _dtoManager.RegisterDTO(query.OutputDTO);
            ValidateFrom(query.From);
            ValidateJoins(query.Joins);
            ValidateSelect(query.Select);
        }

        public void ValidateClauses(SQLQuery query)
        {
            ValidateHaving(query.Having, query.GroupBy);
            ValidateWhere(query.Where);
            ValidateGroupBy(query.GroupBy);
            ValidateOrderBy(query.OrderBy);
        }

        private void ValidateFrom(TableSource from)
        {
            if (from.TableName.Count == 0)
            {
                throw new InvalidOperationException(
                    "FROM table name cannot be empty.");
            }

            _tableManager.RegisterTable(from.Alias, from.Type);
        }

        private void ValidateJoins(List<JoinSource> joins)
        {
            if (joins.Count == 0) return;

            foreach (JoinSource join in joins)
            {
                if (join.Table == null)
                    throw new InvalidOperationException("JOIN table cannot be null.");

                var table = join.Table;

                if (table.TableName.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Join table name cannot be empty for {table.Type.Name}.");
                }

                _tableManager.RegisterTable(join.Table.Alias, join.Table.Type);
                AnalyzeExpression(join.Condition);
            }
        }
        private void ValidateSelect(List<SelectExpression> select)
        {
            foreach (var selected in select)
            {
                AnalyzeExpression(selected);
            }
        }

        private void ValidateWhere(SQLExpression? where)
        {
            if (where != null)
                AnalyzeExpression(where);
        }

        private void ValidateHaving(SQLExpression? having, List<SQLExpression> groupBy)
        {
            if (having == null)
                return;

            if (groupBy == null || groupBy.Count == 0)
            {
                throw new InvalidOperationException(
                    "A HAVING clause cannot be used without a GROUP BY clause.");
            }

            AnalyzeExpression(having);
        }

        private void ValidateGroupBy(List<SQLExpression>? groupBy)
        {
            if (groupBy != null)
            {
                foreach (var group in groupBy)
                {
                    AnalyzeExpression(group);
                }
            }
        }

        private void ValidateOrderBy(List<OrderByExpression>? orderBy)
        {
            if (orderBy != null)
            {
                foreach (var order in orderBy)
                {
                    AnalyzeExpression(order.Expression);
                }
            }
        }

        #endregion

        #region Analyzer
        private SQLExpressionMetadata AnalyzeExpression(SQLExpression expression)
        {
            return expression switch
            {
                SelectExpression select => AnalyzeSelect(select),
                OrderByExpression orderBy => AnalyzeExpression(orderBy.Expression),
                ParenthesizedExpression parenthesized => AnalyzeExpression(parenthesized.Expression),

                BinaryExpression binary => AnalyzeBinary(binary),
                UnaryExpression unary => AnalyzeUnary(unary),

                LikeExpression like => AnalyzeLike(like),
                InExpression @in => AnalyzeIn(@in),
                BetweenExpression between => AnalyzeBetween(between),
                IsNullExpression isnull => AnalyzeIsNull(isnull),

                //Primary
                WildcardExpression wildcard => AnalyzeWildcard(wildcard),
                FunctionExpression function => AnalyzeFunction(function),
                ColumnExpression column => AnalyzeColumn(column),
                LiteralExpression literal => AnalyzeLiteral(literal),
                CaseExpression @case => AnalyzeCase(@case),

                _ => throw new NotSupportedException(
                    ErrorFormatter.Create(
                        context.OriginalText, 
                        expression.Span.Start, 
                        $"Expression '{expression.GetType().Name}' is not supported.", 
                        expression.Span.Length))
            };
        }
        private SQLExpressionMetadata AnalyzeSelect(SelectExpression select)
        {
            var metadata = AnalyzeExpression(select.Expression);

            if (metadata.Type == SQLExpressionType.Unknown)
            {
                var err = ErrorFormatter.Create(
                    context.OriginalText,
                    select.Span.Start,
                    "Cannot determine the type of the SELECT expression.",
                    select.Span.Length);

                throw new SyntaxErrorException(err);
            }
            RegisterSelectOutput(select, metadata);

            return metadata;
        }
        private SQLExpressionMetadata AnalyzeFunction(FunctionExpression function)
        {
            if (function.DeclaredType == SQLFunctionType.Null)
            {
                return new SQLExpressionMetadata
                {
                    Type = SQLExpressionType.NullLiteral,
                    IsNullable = true,
                    IsConstant = true,
                };
            }

            if (function.DeclaredType == SQLFunctionType.Unknown)
            {
                var err = ErrorFormatter.Create(context.OriginalText, function.Span.Start, $"Function '{function.Name}' must declare a return type.", function.Span.Length);
                throw new SyntaxErrorException(err);
            }

            bool isConstant = true;

            foreach (var arg in function.Arguments)
            {
                var metadata = AnalyzeExpression(arg);

                isConstant &= metadata.IsConstant;
            }

            return new SQLExpressionMetadata
            {
                Type = SQLDictionary.GetExpressionType(function.DeclaredType),
                IsNullable = function.IsNullable,
                IsConstant = isConstant,
            };
        }
        private SQLExpressionMetadata AnalyzeUnary(UnaryExpression unary)
        {
            var operand = AnalyzeExpression(unary.Operand);

            if (operand.Type == SQLExpressionType.NullLiteral)
            {
                // +NULL / -NULL / NOT NULL
                return operand with
                {
                    Type = unary.Operator == SQLUnaryOperator.NOT
                        ? SQLExpressionType.Boolean
                        : SQLExpressionType.NullLiteral
                };
            }

            string err = string.Empty;
            switch (unary.Operator)
            {
                
                case SQLUnaryOperator.Positive:
                case SQLUnaryOperator.Negative:
                    if (!SQLDictionary.NumericTypes.Contains(operand.Type))
                    {
                        err = ErrorFormatter.Create(context.OriginalText, unary.Span.Start, $"Unary operator '{unary.Operator}' cannot be applied to type {operand.Type}.", unary.Span.Length);
                        throw new SyntaxErrorException(err);
                    }

                    return operand;

                case SQLUnaryOperator.NOT:
                    if (operand.Type != SQLExpressionType.Boolean)
                    {
                        err = ErrorFormatter.Create(context.OriginalText, unary.Span.Start, $"Logical NOT operator cannot be applied to type {operand.Type}.", unary.Span.Length);
                        throw new SyntaxErrorException(err);
                    }

                    return operand with { Type = SQLExpressionType.Boolean };

                default:
                     err = ErrorFormatter.Create(context.OriginalText, unary.Span.Start, $"Unsupported unary operator '{unary.Operator}'.", unary.Span.Length);
                    throw new SyntaxErrorException(err);
            }
        }
        private SQLExpressionMetadata AnalyzeIsNull(IsNullExpression isn)
        {
            var operandMeta = AnalyzeExpression(isn.Operand);

            if (operandMeta.Type == SQLExpressionType.Unknown)
            {
                var err = ErrorFormatter.Create(context.OriginalText, isn.Span.Start, "Cannot determine expression type for IS NULL.", isn.Span.Length);
                throw new SyntaxErrorException(err);
            }

            return new SQLExpressionMetadata
            {
                Type = SQLExpressionType.Boolean,
                IsNullable = false,
                IsConstant = operandMeta.IsConstant
            };
        }
        private SQLExpressionMetadata AnalyzeBetween(BetweenExpression b)
        {
            var targetMeta = AnalyzeExpression(b.Operand);
            var lowMeta = AnalyzeExpression(b.Lower);
            var highMeta = AnalyzeExpression(b.Upper);

            if (targetMeta.Type == SQLExpressionType.Unknown ||
                lowMeta.Type == SQLExpressionType.Unknown ||
                highMeta.Type == SQLExpressionType.Unknown)
            {
                var err = ErrorFormatter.Create(
                    context.OriginalText,
                    b.Span.Start,
                    "Cannot determine expression types for BETWEEN operator.",
                    b.Span.Length);

                throw new SyntaxErrorException(err);
            }

            SQLExpressionType? knownType = null;

            foreach (var type in new[]
            {
                targetMeta.Type,
                lowMeta.Type,
                highMeta.Type
            })
            {
                if (type == SQLExpressionType.NullLiteral)
                    continue;

                if (knownType == null)
                {
                    knownType = type;
                    continue;
                }

                if (knownType != type)
                {
                    var err = ErrorFormatter.Create(
                        context.OriginalText,
                        b.Span.Start,
                        $"Type mismatch in BETWEEN expression. Cannot compare target of type " +
                        $"'{targetMeta.Type}' against bounds of types '{lowMeta.Type}' and '{highMeta.Type}'.",
                        b.Span.Length);

                    throw new SyntaxErrorException(err);
                }
            }

            return new SQLExpressionMetadata
            {
                Type = SQLExpressionType.Boolean,
                IsNullable = targetMeta.IsNullable ||
                             lowMeta.IsNullable ||
                             highMeta.IsNullable,
                IsConstant = targetMeta.IsConstant &&
                             lowMeta.IsConstant &&
                             highMeta.IsConstant
            };
        }
        private SQLExpressionMetadata AnalyzeIn(InExpression @in)
        {
            var targetMeta = AnalyzeExpression(@in.Operand);

            if (targetMeta.Type == SQLExpressionType.Unknown)
            {
                var err = ErrorFormatter.Create(context.OriginalText, @in.Span.Start, "Cannot determine target expression type for IN operator.", @in.Span.Length);
                throw new SyntaxErrorException(err);
            }

            // Establish our anchor type if the target is known
            SQLExpressionType? knownType = targetMeta.Type != SQLExpressionType.NullLiteral
                ? targetMeta.Type
                : null;

            bool itemsAreNullable = false;
            bool itemsAreConstant = true;

            // 2. Loop through the IN item list
            foreach (var itemExpr in @in.Values)
            {
                var itemMeta = AnalyzeExpression(itemExpr);
                if (itemMeta.Type == SQLExpressionType.Unknown)
                {
                    var err = ErrorFormatter.Create(context.OriginalText, itemExpr.Span.Start, "Cannot determine value type for IN value.", itemExpr.Span.Length);
                    throw new SyntaxErrorException(err);
                }

                // Track propagation properties
                if (itemMeta.IsNullable || itemMeta.Type == SQLExpressionType.NullLiteral) itemsAreNullable = true;
                if (!itemMeta.IsConstant) itemsAreConstant = false;

                if (itemMeta.Type == SQLExpressionType.NullLiteral)
                    continue;

                // NULL has no concrete type, so let the first known item establish the comparison type.
                if (knownType == null)
                {
                    knownType = itemMeta.Type;
                    continue;
                }

                // Catch type conflicts between the target or other items
                if (knownType != itemMeta.Type)
                {
                    var err = ErrorFormatter.Create(
                        context.OriginalText, 
                        itemExpr.Span.Start, 
                        $"Type mismatch in IN list expression. Found conflicting type '{itemMeta.Type}' " +
                        $"against expected type '{knownType}'."
                        , itemExpr.Span.Length);
                    throw new SyntaxErrorException(err);
                }
            }

            return new SQLExpressionMetadata
            {
                Type = SQLExpressionType.Boolean,
                IsNullable = targetMeta.IsNullable || itemsAreNullable,
                IsConstant = targetMeta.IsConstant && itemsAreConstant
            };
        }
        private SQLExpressionMetadata AnalyzeLike(LikeExpression like)
        {
            var operand = AnalyzeExpression(like.Operand);
            var pattern = AnalyzeExpression(like.Pattern);

            if (operand.Type == SQLExpressionType.Unknown || pattern.Type == SQLExpressionType.Unknown)
            {
                var err = ErrorFormatter.Create(context.OriginalText, like.Span.Start, "Cannot determine expression types for LIKE operator.", like.Span.Length);
                throw new SyntaxErrorException(err);
            }

            if (operand.Type != SQLExpressionType.String && operand.Type != SQLExpressionType.NullLiteral)
            {
                var err = ErrorFormatter.Create(context.OriginalText, like.Span.Start, $"LIKE operator cannot be applied to type '{operand.Type}'.", like.Span.Length);
                throw new SyntaxErrorException(err);
            }

            if (pattern.Type != SQLExpressionType.String && pattern.Type != SQLExpressionType.NullLiteral)
            {
                var err = ErrorFormatter.Create(context.OriginalText, like.Span.Start, $"LIKE pattern must be a string, but found '{pattern.Type}'.", like.Span.Length);
                throw new SyntaxErrorException(err);
            }

            return new SQLExpressionMetadata
            {
                Type = SQLExpressionType.Boolean,
                IsNullable = operand.IsNullable || pattern.IsNullable,
                IsConstant = operand.IsConstant && pattern.IsConstant
            };
        }
        private SQLExpressionMetadata AnalyzeCase(CaseExpression caseExpr)
        {
            SQLExpressionMetadata? baseMeta = null;
            SQLExpressionType? baseType = null;

            if (caseExpr.BaseExpression != null)
            {
                baseMeta = AnalyzeExpression(caseExpr.BaseExpression);

                if (baseMeta.Type == SQLExpressionType.Unknown)
                {
                    var err = ErrorFormatter.Create(
                        context.OriginalText,
                        caseExpr.BaseExpression.Span.Start,
                        "Cannot determine CASE base expression type.",
                        caseExpr.BaseExpression.Span.Length);

                    throw new SyntaxErrorException(err);
                }

                if (baseMeta.Type != SQLExpressionType.NullLiteral)
                    baseType = baseMeta.Type;
            }

            SQLExpressionType? returnType = null;
            bool returnNullable = false;
            bool combinedConstant = baseMeta?.IsConstant ?? true;

            foreach (var caseWhen in caseExpr.Conditions)
            {
                var whenMeta = AnalyzeExpression(caseWhen.Condition);
                var thenMeta = AnalyzeExpression(caseWhen.Result);

                if (whenMeta.Type == SQLExpressionType.Unknown)
                {
                    var err = ErrorFormatter.Create(
                        context.OriginalText,
                        caseWhen.Condition.Span.Start,
                        "Cannot determine WHEN expression type.",
                        caseWhen.Condition.Span.Length);

                    throw new SyntaxErrorException(err);
                }

                if (thenMeta.Type == SQLExpressionType.Unknown)
                {
                    var err = ErrorFormatter.Create(
                        context.OriginalText,
                        caseWhen.Result.Span.Start,
                        "Cannot determine return type for THEN.",
                        caseWhen.Result.Span.Length);

                    throw new SyntaxErrorException(err);
                }

                combinedConstant &= whenMeta.IsConstant && thenMeta.IsConstant;

                // Simple CASE: CASE base WHEN value
                if (baseType != null &&
                    whenMeta.Type != SQLExpressionType.NullLiteral &&
                    whenMeta.Type != baseType)
                {
                    var err = ErrorFormatter.Create(
                        context.OriginalText,
                        caseWhen.Condition.Span.Start,
                        $"Cannot compare CASE base type '{baseType}' with WHEN type '{whenMeta.Type}'.",
                        caseWhen.Condition.Span.Length);

                    throw new SyntaxErrorException(err);
                }

                // Searched CASE: CASE WHEN condition
                if (baseMeta == null &&
                    whenMeta.Type != SQLExpressionType.Boolean &&
                    whenMeta.Type != SQLExpressionType.NullLiteral)
                {
                    var err = ErrorFormatter.Create(
                        context.OriginalText,
                        caseWhen.Condition.Span.Start,
                        $"WHEN condition must be Boolean, but found '{whenMeta.Type}'.",
                        caseWhen.Condition.Span.Length);

                    throw new SyntaxErrorException(err);
                }

                // Result NULL affects nullability, but not the concrete result type.
                if (thenMeta.Type == SQLExpressionType.NullLiteral)
                {
                    returnNullable = true;
                    continue;
                }

                returnNullable |= thenMeta.IsNullable;

                if (returnType == null)
                {
                    returnType = thenMeta.Type;
                }
                else if (returnType != thenMeta.Type)
                {
                    var err = ErrorFormatter.Create(
                        context.OriginalText,
                        caseWhen.Result.Span.Start,
                        $"Cannot unify CASE return types '{returnType}' and '{thenMeta.Type}'.",
                        caseWhen.Result.Span.Length);

                    throw new SyntaxErrorException(err);
                }
            }

            // ELSE, if present, participates in exactly the same result-type logic.
            if (caseExpr.ElseExpression != null)
            {
                var elseMeta = AnalyzeExpression(caseExpr.ElseExpression);

                if (elseMeta.Type == SQLExpressionType.Unknown)
                {
                    var err = ErrorFormatter.Create(
                        context.OriginalText,
                        caseExpr.ElseExpression.Span.Start,
                        "Cannot determine return type for ELSE.",
                        caseExpr.ElseExpression.Span.Length);

                    throw new SyntaxErrorException(err);
                }

                combinedConstant &= elseMeta.IsConstant;

                if (elseMeta.Type == SQLExpressionType.NullLiteral)
                {
                    returnNullable = true;
                }
                else
                {
                    returnNullable |= elseMeta.IsNullable;

                    if (returnType == null)
                    {
                        returnType = elseMeta.Type;
                    }
                    else if (returnType != elseMeta.Type)
                    {
                        var err = ErrorFormatter.Create(
                            context.OriginalText,
                            caseExpr.ElseExpression.Span.Start,
                            $"Cannot unify CASE return types '{returnType}' and '{elseMeta.Type}'.",
                            caseExpr.ElseExpression.Span.Length);

                        throw new SyntaxErrorException(err);
                    }
                }
            }
            else
            {
                // No ELSE means CASE can return NULL when no WHEN matches.
                returnNullable = true;
            }

            return new SQLExpressionMetadata
            {
                Type = returnType ?? SQLExpressionType.NullLiteral,
                IsNullable = returnNullable || returnType == null,
                IsConstant = combinedConstant
            };
        }
        private SQLExpressionMetadata AnalyzeBinary(BinaryExpression binary)
        {
            var op = binary.Operator;

            if (IsArithmeticOperator(op))
                return AnalyzeArithmetic(binary);
            else if (IsComparisonOperator(op))
                return AnalyzeComparison(binary);
            else if (IsLogicalOperator(op))
                return AnalyzeLogical(binary);
            else if (IsBitwiseOperator(op))
                return AnalyzeBitwise(binary);

            throw new UnreachableException($"Unhandled binary operator: {op}");

            bool IsArithmeticOperator(SQLBinaryOperator op)
            {
                return op == SQLBinaryOperator.Add ||
                       op == SQLBinaryOperator.Subtract ||
                       op == SQLBinaryOperator.Multiply ||
                       op == SQLBinaryOperator.Divide ||
                       op == SQLBinaryOperator.Modulo;
            }
            bool IsComparisonOperator(SQLBinaryOperator op)
            {
                return op == SQLBinaryOperator.Equal ||
                       op == SQLBinaryOperator.NotEqual ||
                       op == SQLBinaryOperator.LessThan ||
                       op == SQLBinaryOperator.LessThanOrEqual ||
                       op == SQLBinaryOperator.GreaterThan ||
                       op == SQLBinaryOperator.GreaterThanOrEqual;
            }
            bool IsLogicalOperator(SQLBinaryOperator op)
            {
                return op == SQLBinaryOperator.AND ||
                       op == SQLBinaryOperator.OR;
            }
            bool IsBitwiseOperator(SQLBinaryOperator op)
            {
                return op == SQLBinaryOperator.BitwiseAnd ||
                       op == SQLBinaryOperator.BitwiseOr ||
                       op == SQLBinaryOperator.BitwiseXor ||
                       op == SQLBinaryOperator.BitwiseLeftShift ||
                       op == SQLBinaryOperator.BitwiseRightShift;
            }
        }
        private SQLExpressionMetadata AnalyzeBitwise(BinaryExpression binary)
        {
            var left = AnalyzeExpression(binary.Left);
            var right = AnalyzeExpression(binary.Right);
            var op = binary.Operator;

            bool combinedNullable = left.IsNullable || right.IsNullable;
            bool combinedConstant = left.IsConstant && right.IsConstant;

            string err;

            if (left.Type == SQLExpressionType.Unknown || right.Type == SQLExpressionType.Unknown)
            {
                err = ErrorFormatter.Create(
                    context.OriginalText,
                    binary.Span.Start,
                    $"Cannot determine operand types for bitwise operator '{op}'.",
                    binary.Span.Length);

                throw new SyntaxErrorException(err);
            }

            err = ErrorFormatter.Create(
                    context.OriginalText,
                    binary.Span.Start,
                    $"Bitwise operator '{op}' cannot be applied to types '{left.Type}' and '{right.Type}'.",
                    binary.Span.Length);

            return left with
            {
                Type = GetReturnType(left.Type, right.Type, err),
                IsNullable = combinedNullable,
                IsConstant = combinedConstant
            };

            static SQLExpressionType GetReturnType(SQLExpressionType left, SQLExpressionType right, string error)
            {
                switch(left, right)
                {
                    case (SQLExpressionType.Integer,  SQLExpressionType.Integer):
                        return SQLExpressionType.Integer;
                    case (SQLExpressionType.Binary, SQLExpressionType.Binary):
                        return SQLExpressionType.Binary;
                    case (SQLExpressionType.NullLiteral, SQLExpressionType.Integer):
                        return SQLExpressionType.Integer;
                    case (SQLExpressionType.Integer, SQLExpressionType.NullLiteral):
                        return SQLExpressionType.Integer;
                    case (SQLExpressionType.NullLiteral, SQLExpressionType.Binary):
                        return SQLExpressionType.Binary;
                    case (SQLExpressionType.Binary, SQLExpressionType.NullLiteral):
                        return SQLExpressionType.Binary;
                    case (SQLExpressionType.NullLiteral, SQLExpressionType.NullLiteral):
                        return SQLExpressionType.NullLiteral;
                    default:
                        throw new SyntaxErrorException(error);
                }
            }
        }
        private SQLExpressionMetadata AnalyzeLogical(BinaryExpression binary)
        {
            var left = AnalyzeExpression(binary.Left);
            var right = AnalyzeExpression(binary.Right);
            var op = binary.Operator;

            bool combinedNullable = left.IsNullable || right.IsNullable;
            bool combinedConstant = left.IsConstant && right.IsConstant;

            string err;

            if (left.Type == SQLExpressionType.Unknown || right.Type == SQLExpressionType.Unknown)
            {
                err = ErrorFormatter.Create(
                    context.OriginalText,
                    binary.Span.Start,
                    $"Cannot determine operand types for logical operator '{op}'.",
                    binary.Span.Length);

                throw new SyntaxErrorException(err);
            }

            SQLExpressionType returnType;

            returnType = (left.Type, right.Type) switch
            {
                (SQLExpressionType.Boolean, SQLExpressionType.Boolean) => SQLExpressionType.Boolean,
                (SQLExpressionType.Boolean, SQLExpressionType.NullLiteral) => SQLExpressionType.Boolean,
                (SQLExpressionType.NullLiteral, SQLExpressionType.Boolean) => SQLExpressionType.Boolean,
                (SQLExpressionType.NullLiteral, SQLExpressionType.NullLiteral) => SQLExpressionType.NullLiteral,
                _ => throw new SyntaxErrorException(
                    err = ErrorFormatter.Create(
                    context.OriginalText,
                    binary.Span.Start,
                    $"Logical operator '{op}' cannot be applied to " +
                    $"types '{left.Type}' and '{right.Type}'.",
                    binary.Span.Length))
            };

            return left with
            {
                Type = returnType,
                IsNullable = combinedNullable,
                IsConstant = combinedConstant
            };
        }
        private SQLExpressionMetadata AnalyzeComparison(BinaryExpression binary)
        {
            var left = AnalyzeExpression(binary.Left);
            var right = AnalyzeExpression(binary.Right);
            var op = binary.Operator;

            bool combinedNullable = left.IsNullable || right.IsNullable;
            bool combinedConstant = left.IsConstant && right.IsConstant;

            string err;

            if (left.Type == SQLExpressionType.Unknown || right.Type == SQLExpressionType.Unknown)
            {
                err = ErrorFormatter.Create(
                    context.OriginalText,
                    binary.Span.Start,
                    $"Cannot determine operand types for comparison operator '{op}'.",
                    binary.Span.Length);

                throw new SyntaxErrorException(err);
            }

            if (left.Type != right.Type &&
                left.Type != SQLExpressionType.NullLiteral &&
                right.Type != SQLExpressionType.NullLiteral)
            {
                err = ErrorFormatter.Create(
                    context.OriginalText,
                    binary.Span.Start,
                    $"Comparison operator '{op}' cannot be applied to types " +
                    $"'{left.Type}' and '{right.Type}'.",
                    binary.Span.Length);

                throw new SyntaxErrorException(err);
            }

            return left with
            {
                Type = SQLExpressionType.Boolean,
                IsNullable = combinedNullable,
                IsConstant = combinedConstant
            };
        }
        private SQLExpressionMetadata AnalyzeArithmetic(BinaryExpression binary)
        {
            var left = AnalyzeExpression(binary.Left);
            var right = AnalyzeExpression(binary.Right);
            var op = binary.Operator;

            string err;

            if (left.Type == SQLExpressionType.Unknown ||
                right.Type == SQLExpressionType.Unknown)
            {
                err = ErrorFormatter.Create(
                    context.OriginalText,
                    binary.Span.Start,
                    $"Cannot determine operand types for arithmetic operator '{op}'.",
                    binary.Span.Length);

                throw new SyntaxErrorException(err);
            }

            SQLExpressionType returnType;

            if (left.Type == SQLExpressionType.NullLiteral && right.Type == SQLExpressionType.NullLiteral)
            {
                returnType = SQLExpressionType.NullLiteral;
            }
            else if (left.Type == SQLExpressionType.NullLiteral)
            {
                if (!SQLDictionary.NumericTypes.Contains(right.Type))
                {
                    err = ErrorFormatter.Create(
                        context.OriginalText,
                        binary.Span.Start,
                        $"Arithmetic operator '{op}' cannot be applied to NULL and {right.Type}.",
                        binary.Span.Length);

                    throw new SyntaxErrorException(err);
                }


                returnType = right.Type;
            }
            else if (right.Type == SQLExpressionType.NullLiteral)
            {
                if (!SQLDictionary.NumericTypes.Contains(left.Type))
                {
                    err = ErrorFormatter.Create(
                        context.OriginalText,
                        binary.Span.Start,
                        $"Arithmetic operator '{op}' cannot be applied to {left.Type} and NULL.",
                        binary.Span.Length);

                    throw new SyntaxErrorException(err);
                }

                returnType = left.Type;
            }
            else if (SQLDictionary.NumericTypes.Contains(left.Type) && SQLDictionary.NumericTypes.Contains(right.Type))
            {
                returnType = PromoteNumeric(left.Type, right.Type);
            }
            else
            {
                err = ErrorFormatter.Create(
                    context.OriginalText,
                    binary.Span.Start,
                    $"Arithmetic operator '{op}' cannot be applied to " +
                    $"types '{left.Type}' and '{right.Type}'.",
                    binary.Span.Length);

                throw new SyntaxErrorException(err);
            }

            return left with
            {
                Type = returnType,
                IsNullable = left.IsNullable || right.IsNullable,
                IsConstant = left.IsConstant && right.IsConstant
            };

            static SQLExpressionType PromoteNumeric(SQLExpressionType left, SQLExpressionType right)
            {
                if (left == SQLExpressionType.Double ||
                    right == SQLExpressionType.Double)
                    return SQLExpressionType.Double;

                if (left == SQLExpressionType.Decimal ||
                    right == SQLExpressionType.Decimal)
                    return SQLExpressionType.Decimal;

                return SQLExpressionType.Integer;
            }
        }
        private SQLExpressionMetadata AnalyzeLiteral(LiteralExpression literal)
        {
            if (literal.Type == SQLLiteralType.UnicodeString)
            {
                return new SQLExpressionMetadata
                {
                    Type = SQLExpressionType.String,
                    ClrType = typeof(string),
                    IsConstant = true,
                    IsNullable = false,
                };
            }
            else if (literal.Type == SQLLiteralType.Date)
            {
                return new SQLExpressionMetadata
                {
                    Type = SQLExpressionType.Date,
                    ClrType = typeof(DateOnly),
                    IsConstant = true,
                    IsNullable = false
                };
            }
            else if (literal.Type == SQLLiteralType.Time)
            {
                return new SQLExpressionMetadata
                {
                    Type = SQLExpressionType.Time,
                    ClrType = typeof(TimeOnly),
                    IsConstant = true,
                    IsNullable = false
                };
            }
            else if (literal.Type == SQLLiteralType.Datetime)
            {
                return new SQLExpressionMetadata
                {
                    Type = SQLExpressionType.DateTime,
                    ClrType = typeof(DateTime),
                    IsConstant = true,
                    IsNullable = false
                };
            }
            else if (literal.Type == SQLLiteralType.String)
            {
                return new SQLExpressionMetadata
                {
                    Type = SQLExpressionType.String,
                    ClrType = typeof(string),
                    IsConstant = true,
                    IsNullable = false,
                };
            }
            else if (literal.Type == SQLLiteralType.Integer)
            {
                return new SQLExpressionMetadata
                {
                    Type = SQLExpressionType.Integer,
                    ClrType = typeof(int),
                    IsConstant = true,
                    IsNullable = false,
                };
            }
            else if (literal.Type == SQLLiteralType.Scientific)
            {
                return new SQLExpressionMetadata
                {
                    Type = SQLExpressionType.Double,
                    ClrType = typeof(double),
                    IsConstant = true,
                    IsNullable = false,
                };
            }
            else if (literal.Type == SQLLiteralType.Decimal)
            {
                return new SQLExpressionMetadata
                {
                    Type = SQLExpressionType.Decimal,
                    ClrType = typeof(decimal),
                    IsConstant = true,
                    IsNullable = false,
                };
            }
            else if (literal.Type == SQLLiteralType.BinaryLiteral)
            {
                return new SQLExpressionMetadata
                {
                    Type = SQLExpressionType.Binary,
                    ClrType = typeof(byte[]),
                    IsConstant = true,
                    IsNullable = false,
                };
            }
            else if (literal.Type == SQLLiteralType.Money)
            {
                return new SQLExpressionMetadata
                {
                    Type = SQLExpressionType.Decimal,
                    ClrType = typeof(decimal),
                    IsConstant = true,
                    IsNullable = false,
                };
            }
            else if (literal.Type == SQLLiteralType.Null)
            {
                return new SQLExpressionMetadata
                {
                    Type = SQLExpressionType.NullLiteral,
                    ClrType = null,
                    IsConstant = true,
                    IsNullable = true,
                };
            }
            var err = ErrorFormatter.Create(
                context.OriginalText,
                literal.Span.Start,
                $"Unexpected literal Type '{literal.Value}' ({literal.Type})",
                literal.Span.Length);

            throw new SyntaxErrorException(err);
        }
        private SQLExpressionMetadata AnalyzeWildcard(WildcardExpression wildcard)
        {
            if (wildcard.TableAlias != null)
            {
                if (wildcard.TableAlias.Count > 1)
                {
                    var err = ErrorFormatter.Create(
                        context.OriginalText,
                        wildcard.Span.Start,
                        $"Invalid column reference '{string.Join(".", wildcard.TableAlias)}.*'. " +
                        "Only a single table alias is supported.",
                        wildcard.Span.Length);

                    throw new SyntaxErrorException(err);
                }
            }

            return new SQLExpressionMetadata
            {
                Type = SQLExpressionType.Unknown,
                IsWildcard = true,
                IsNullable = false,
                IsConstant = false
            };
        }
        private SQLExpressionMetadata AnalyzeColumn(ColumnExpression column)
        {
            if (column.Table == null)
            {
                var err = ErrorFormatter.Create(
                    context.OriginalText,
                    column.Span.Start,
                    $"Column must be a qualified column name!",
                    column.Span.Length);

                throw new ArgumentNullException(err);
            }

            if (column.Table.Count > 1)
            {
                var err = ErrorFormatter.Create(
                    context.OriginalText,
                    column.Span.Start,
                    $"Invalid column reference '{string.Join(".", column.Table)}.{column.Column}'. " +
                    "Only a single table alias is supported.",
                    column.Span.Length);

                throw new SyntaxErrorException(err);
            }

            var metadata = _tableManager.GetColumnMetadata(column.Table[0], column.Column);

            return new SQLExpressionMetadata
            {
                IsConstant = false,
                IsNullable = metadata.IsNullable,
                Type = metadata.ExpressionType,
            };
        }
        #endregion
        private void RegisterSelectOutput(SelectExpression select, SQLExpressionMetadata metadata)
        {
            if (select.Alias != null)
            {
                _dtoManager.RegisterOutput(select.Alias, metadata);
                return;
            }

            if (select.Expression is ColumnExpression column)
            {
                _dtoManager.RegisterOutput(column.Column);
                return;
            }

            if (select.Expression is WildcardExpression wildcard)
            {
                if (wildcard.TableAlias != null)
                {
                    var columns = _tableManager.GetTableColumns(wildcard.TableAlias[0]);

                    foreach (var c in columns)
                        _dtoManager.RegisterOutput(c.Key);
                }
                else
                {
                    var tables = _tableManager.GetAllTables();

                    foreach (var table in tables.Values)
                    {
                        foreach (var c in table)
                            _dtoManager.RegisterOutput(c.Key);
                    }
                }
                return;
            }
            var err = ErrorFormatter.Create(
                context.OriginalText,
                select.Span.Start,
                $"A non-column SELECT expression must have an alias.",
                select.Span.Length);
            throw new SyntaxErrorException(err);
        }

        #region Testing
        public SQLExpressionMetadata DebugAnalyzeExpression(SQLExpression expression)
        {
            return AnalyzeExpression(expression);
        }

        #region Testing Helper Seeds
        public void SeedTable(string tableAlias, Type dtoType)
        {
            _tableManager.RegisterTable(tableAlias, dtoType);
        }
        public void SeedDTO(Type dtoType)
        {
            _dtoManager.RegisterDTO(dtoType);
        }
        #endregion
        #endregion
    }
}

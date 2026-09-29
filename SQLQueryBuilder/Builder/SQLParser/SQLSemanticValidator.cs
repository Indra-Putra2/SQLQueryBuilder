using SQLQueryBuilder.Core.SQLEnum.SemanticValidator;
using SQLQueryBuilder.Core.SQLEnum.SQLExpression;
using SQLQueryBuilder.Core.SQLExpression;
using SQLQueryBuilder.Helper;
using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.Context;
using SQLQueryBuilder.Model.SQL_Expression;
using System.Data;
using System.Diagnostics;

namespace SQLQueryBuilder.Builder.SQLParser
{
    internal static class SQLSemanticValidator
    {
        #region Validation
        public static void ValidateFrom(TableSource from, ref SQLValidatorContext context)
        {
            if (from == null)
                throw new NullReferenceException("FROM table name cannot be null.");

            if (from.TableName.Count == 0)
                throw new InvalidOperationException("FROM table name cannot be empty.");

            context.TableManager.RegisterTable(from.Alias.Value, from.Type);
        }
        public static void ValidateJoins(JoinSource join, ref SQLValidatorContext context)
        {
            if (join == null)
                throw new NullReferenceException("JOIN table cannot be null.");

            var table = join.Table;

            if (table.TableName.Count == 0)
                throw new InvalidOperationException($"Join table name cannot be empty for {table.Type.Name}.");

            context.TableManager.RegisterTable(join.Table.Alias.Value, join.Table.Type);
            AnalyzeExpression(join.Condition, ref context);
        }
        public static void ValidateSelect(List<SelectExpression> select, ref SQLValidatorContext context)
        {
            if (select == null) throw new NullReferenceException("Select cannot be null.");
            if (select.Count == 0) throw new InvalidOperationException("Select cannot be empty.");
            foreach (var selected in select)
            {
                AnalyzeExpression(selected, ref context);
            }
        }
        public static void ValidateWhere(SQLExpression where, ref SQLValidatorContext context)
        {
            if (where == null) return;
            var metadata = AnalyzeExpression(where, ref context);

            if (metadata.Type != SQLExpressionType.Boolean)
                throw new SyntaxErrorException("WHERE clause must result in a boolean!");
        }
        public static void ValidateHaving(SQLExpression having, List<SQLExpression>? groupBy, ref SQLValidatorContext context)
        {
            if (having == null) return;

            if (groupBy == null || groupBy.Count == 0)
            {
                throw new SyntaxErrorException(
                    "A HAVING clause cannot be used without a GROUP BY clause.");
            }
            var metadata = AnalyzeExpression(having, ref context);

            if (metadata.Type != SQLExpressionType.Boolean)
                throw new SyntaxErrorException("HAVING clause must result in a boolean!");
        }
        public static void ValidateGroupBy(List<SQLExpression> groupBy, ref SQLValidatorContext context)
        {
            if (groupBy == null || groupBy.Count == 0)
                return;

            foreach (var group in groupBy)
            {
                AnalyzeExpression(group, ref context);
            }
        }
        public static void ValidateOrderBy(List<OrderByExpression> orderBy, ref SQLValidatorContext context)
        {
            if (orderBy == null || orderBy.Count == 0)
                return;

            foreach (var order in orderBy)
            {
                AnalyzeExpression(order, ref context);
            }
        }
        #endregion

        #region Analyzer
        public static SQLExpressionMetadata AnalyzeExpression(SQLExpression expression, ref SQLValidatorContext context)
        {
            return expression switch
            {
                SelectExpression select => AnalyzeSelect(select, ref context),
                OrderByExpression orderBy => AnalyzeExpression(orderBy.Expression, ref context),
                ParenthesizedExpression parenthesized => AnalyzeExpression(parenthesized.Expression, ref context),

                BinaryExpression binary => AnalyzeBinary(binary, ref context),
                UnaryExpression unary => AnalyzeUnary(unary, ref context),

                LikeExpression like => AnalyzeLike(like, ref context),
                InExpression @in => AnalyzeIn(@in, ref context),
                BetweenExpression between => AnalyzeBetween(between, ref context),
                IsNullExpression isnull => AnalyzeIsNull(isnull, ref context),

                //Primary
                WildcardExpression wildcard => AnalyzeWildcard(wildcard, ref context),
                FunctionExpression function => AnalyzeFunction(function, ref context),
                ColumnExpression column => AnalyzeColumn(column, ref context),
                LiteralExpression literal => AnalyzeLiteral(literal, ref context),
                CaseExpression @case => AnalyzeCase(@case, ref context),

                _ => throw new NotSupportedException(
                    ErrorFormatter.Create(
                        context.Span.ToString(),
                        expression.Span.Start,
                        $"Expression '{expression.GetType().Name}' is not supported.",
                        expression.Span.Length))
            };
        }
        private static SQLExpressionMetadata AnalyzeSelect(SelectExpression select, ref SQLValidatorContext context)
        {
            var metadata = AnalyzeExpression(select.Expression, ref context);

            if (metadata.Type == SQLExpressionType.Unknown && !metadata.IsWildcard)
            {
                var err = ErrorFormatter.Create(
                    context.Span.ToString(),
                    select.Span.Start,
                    "Cannot determine the type of the SELECT expression.",
                    select.Span.Length);

                throw new SyntaxErrorException(err);
            }
            RegisterSelectOutput(select, metadata, ref context);

            return metadata;
        }
        private static SQLExpressionMetadata AnalyzeFunction(FunctionExpression function, ref SQLValidatorContext context)
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
                var err = ErrorFormatter.Create(
                    context.Span.ToString(),
                    function.Span.Start,
                    $"Function '{function.Name}' must declare a return type. e.g.: 'FUNC() TYPE INT? CONST'",
                    function.Span.Length);
                throw new SyntaxErrorException(err);
            }

            foreach (var arg in function.Arguments)
            {
                var metadata = AnalyzeExpression(arg, ref context);
            }

            return new SQLExpressionMetadata
            {
                Type = SQLDictionary.GetExpressionType(function.DeclaredType),
                IsNullable = function.IsNullable,
                IsConstant = function.IsConstant,
            };
        }
        private static SQLExpressionMetadata AnalyzeUnary(UnaryExpression unary, ref SQLValidatorContext context)
        {
            var operand = AnalyzeExpression(unary.Operand, ref context);

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
                        err = ErrorFormatter.Create(context.Span.ToString(), unary.Span.Start, $"Unary operator '{unary.Operator}' cannot be applied to type {operand.Type}.", unary.Span.Length);
                        throw new SyntaxErrorException(err);
                    }

                    return operand;

                case SQLUnaryOperator.NOT:
                    if (operand.Type != SQLExpressionType.Boolean)
                    {
                        err = ErrorFormatter.Create(context.Span.ToString(), unary.Span.Start, $"Logical NOT operator cannot be applied to type {operand.Type}.", unary.Span.Length);
                        throw new SyntaxErrorException(err);
                    }

                    return operand with { Type = SQLExpressionType.Boolean };

                default:
                    err = ErrorFormatter.Create(context.Span.ToString(), unary.Span.Start, $"Unsupported unary operator '{unary.Operator}'.", unary.Span.Length);
                    throw new SyntaxErrorException(err);
            }
        }
        private static SQLExpressionMetadata AnalyzeIsNull(IsNullExpression isn, ref SQLValidatorContext context)
        {
            var operandMeta = AnalyzeExpression(isn.Operand, ref context);

            if (operandMeta.Type == SQLExpressionType.Unknown)
            {
                var err = ErrorFormatter.Create(context.Span.ToString(), isn.Span.Start, "Cannot determine expression type for IS NULL.", isn.Span.Length);
                throw new SyntaxErrorException(err);
            }

            return new SQLExpressionMetadata
            {
                Type = SQLExpressionType.Boolean,
                IsNullable = false,
                IsConstant = operandMeta.IsConstant
            };
        }
        private static SQLExpressionMetadata AnalyzeBetween(BetweenExpression b, ref SQLValidatorContext context)
        {
            var targetMeta = AnalyzeExpression(b.Operand, ref context);
            var lowMeta = AnalyzeExpression(b.Lower, ref context);
            var highMeta = AnalyzeExpression(b.Upper, ref context);

            if (targetMeta.Type == SQLExpressionType.Unknown ||
                lowMeta.Type == SQLExpressionType.Unknown ||
                highMeta.Type == SQLExpressionType.Unknown)
            {
                var err = ErrorFormatter.Create(
                    context.Span.ToString(),
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

                if (!AreTypesCompatible(knownType.Value, type))
                {
                    var err = ErrorFormatter.Create(
                        context.Span.ToString(),
                        b.Span.Start,
                        $"Type mismatch in BETWEEN expression.\n" +
                        $"Target is '{targetMeta.Type}' but lower type is " +
                        $"'{lowMeta.Type}' and upper type is '{highMeta.Type}'.",
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
        private static SQLExpressionMetadata AnalyzeIn(InExpression @in, ref SQLValidatorContext context)
        {
            var targetMeta = AnalyzeExpression(@in.Operand, ref context);

            if (targetMeta.Type == SQLExpressionType.Unknown)
            {
                var err = ErrorFormatter.Create(context.Span.ToString(), @in.Span.Start, "Cannot determine target expression type for IN operator.", @in.Span.Length);
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
                var itemMeta = AnalyzeExpression(itemExpr, ref context);
                if (itemMeta.Type == SQLExpressionType.Unknown)
                {
                    var err = ErrorFormatter.Create(context.Span.ToString(), itemExpr.Span.Start, "Cannot determine value type for IN value.", itemExpr.Span.Length);
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
                if (!AreTypesCompatible(knownType.Value, itemMeta.Type))
                {
                    var err = ErrorFormatter.Create(
                        context.Span.ToString(),
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
        private static SQLExpressionMetadata AnalyzeLike(LikeExpression like, ref SQLValidatorContext context)
        {
            var operand = AnalyzeExpression(like.Operand, ref context);
            var pattern = AnalyzeExpression(like.Pattern, ref context);

            if (operand.Type == SQLExpressionType.Unknown || pattern.Type == SQLExpressionType.Unknown)
            {
                var err = ErrorFormatter.Create(context.Span.ToString(), like.Span.Start, "Cannot determine expression types for LIKE operator.", like.Span.Length);
                throw new SyntaxErrorException(err);
            }

            if (operand.Type != SQLExpressionType.String && operand.Type != SQLExpressionType.NullLiteral)
            {
                var err = ErrorFormatter.Create(context.Span.ToString(), like.Span.Start, $"LIKE operator cannot be applied to type '{operand.Type}'.", like.Span.Length);
                throw new SyntaxErrorException(err);
            }

            if (pattern.Type != SQLExpressionType.String && pattern.Type != SQLExpressionType.NullLiteral)
            {
                var err = ErrorFormatter.Create(context.Span.ToString(), like.Span.Start, $"LIKE pattern must be a string, but found '{pattern.Type}'.", like.Span.Length);
                throw new SyntaxErrorException(err);
            }

            return new SQLExpressionMetadata
            {
                Type = SQLExpressionType.Boolean,
                IsNullable = operand.IsNullable || pattern.IsNullable,
                IsConstant = operand.IsConstant && pattern.IsConstant
            };
        }
        private static SQLExpressionMetadata AnalyzeCase(CaseExpression caseExpr, ref SQLValidatorContext context)
        {
            SQLExpressionMetadata? baseMeta = null;
            SQLExpressionType? baseType = null;

            if (caseExpr.BaseExpression != null)
            {
                baseMeta = AnalyzeExpression(caseExpr.BaseExpression, ref context);

                if (baseMeta.Type == SQLExpressionType.Unknown)
                {
                    var err = ErrorFormatter.Create(
                        context.Span.ToString(),
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
                var whenMeta = AnalyzeExpression(caseWhen.Condition, ref context);
                var thenMeta = AnalyzeExpression(caseWhen.Result, ref context);

                if (whenMeta.Type == SQLExpressionType.Unknown)
                {
                    var err = ErrorFormatter.Create(
                        context.Span.ToString(),
                        caseWhen.Condition.Span.Start,
                        "Cannot determine WHEN expression type.",
                        caseWhen.Condition.Span.Length);

                    throw new SyntaxErrorException(err);
                }

                if (thenMeta.Type == SQLExpressionType.Unknown)
                {
                    var err = ErrorFormatter.Create(
                        context.Span.ToString(),
                        caseWhen.Result.Span.Start,
                        "Cannot determine return type for THEN.",
                        caseWhen.Result.Span.Length);

                    throw new SyntaxErrorException(err);
                }

                combinedConstant &= whenMeta.IsConstant && thenMeta.IsConstant;

                // Simple CASE: CASE base WHEN value
                if (baseType != null &&
                    whenMeta.Type != SQLExpressionType.NullLiteral &&
                    !AreTypesCompatible(baseType.Value, whenMeta.Type))
                {
                    var err = ErrorFormatter.Create(
                        context.Span.ToString(),
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
                        context.Span.ToString(),
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
                else if (!AreTypesCompatible(returnType.Value, thenMeta.Type))
                {
                    var err = ErrorFormatter.Create(
                        context.Span.ToString(),
                        caseWhen.Result.Span.Start,
                        $"Cannot unify CASE return types '{returnType}' and '{thenMeta.Type}'.",
                        caseWhen.Result.Span.Length);

                    throw new SyntaxErrorException(err);
                }
            }

            // ELSE, if present, participates in exactly the same result-type logic.
            if (caseExpr.ElseExpression != null)
            {
                var elseMeta = AnalyzeExpression(caseExpr.ElseExpression, ref context);

                if (elseMeta.Type == SQLExpressionType.Unknown)
                {
                    var err = ErrorFormatter.Create(
                        context.Span.ToString(),
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
                    else if (!AreTypesCompatible(returnType.Value, elseMeta.Type))
                    {
                        var err = ErrorFormatter.Create(
                            context.Span.ToString(),
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
        private static SQLExpressionMetadata AnalyzeBinary(BinaryExpression binary, ref SQLValidatorContext context)
        {
            var op = binary.Operator;

            if (IsArithmeticOperator(op))
                return AnalyzeArithmetic(binary, ref context);
            else if (IsComparisonOperator(op))
                return AnalyzeComparison(binary, ref context);
            else if (IsLogicalOperator(op))
                return AnalyzeLogical(binary, ref context);
            else if (IsBitwiseOperator(op))
                return AnalyzeBitwise(binary, ref context);

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
        private static SQLExpressionMetadata AnalyzeBitwise(BinaryExpression binary, ref SQLValidatorContext context)
        {
            var left = AnalyzeExpression(binary.Left, ref context);
            var right = AnalyzeExpression(binary.Right, ref context);
            var op = binary.Operator;

            bool combinedNullable = left.IsNullable || right.IsNullable;
            bool combinedConstant = left.IsConstant && right.IsConstant;

            string err;

            if (left.Type == SQLExpressionType.Unknown || right.Type == SQLExpressionType.Unknown)
            {
                err = ErrorFormatter.Create(
                    context.Span.ToString(),
                    binary.Span.Start,
                    $"Cannot determine operand types for bitwise operator '{op}'.",
                    binary.Span.Length);

                throw new SyntaxErrorException(err);
            }

            err = ErrorFormatter.Create(
                    context.Span.ToString(),
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
                switch (left, right)
                {
                    case (SQLExpressionType.Integer, SQLExpressionType.Integer):
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
        private static SQLExpressionMetadata AnalyzeLogical(BinaryExpression binary, ref SQLValidatorContext context)
        {
            var left = AnalyzeExpression(binary.Left, ref context);
            var right = AnalyzeExpression(binary.Right, ref context);
            var op = binary.Operator;

            bool combinedNullable = left.IsNullable || right.IsNullable;
            bool combinedConstant = left.IsConstant && right.IsConstant;

            string err;

            if (left.Type == SQLExpressionType.Unknown || right.Type == SQLExpressionType.Unknown)
            {
                err = ErrorFormatter.Create(
                    context.Span.ToString(),
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
                    context.Span.ToString(),
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
        private static SQLExpressionMetadata AnalyzeComparison(BinaryExpression binary, ref SQLValidatorContext context)
        {
            var left = AnalyzeExpression(binary.Left, ref context);
            var right = AnalyzeExpression(binary.Right, ref context);
            var op = binary.Operator;

            bool combinedNullable = left.IsNullable || right.IsNullable;
            bool combinedConstant = left.IsConstant && right.IsConstant;

            string err;

            if (left.Type == SQLExpressionType.Unknown || right.Type == SQLExpressionType.Unknown)
            {
                err = ErrorFormatter.Create(
                    context.Span.ToString(),
                    binary.Span.Start,
                    $"Cannot determine operand types for comparison operator '{op}'.",
                    binary.Span.Length);

                throw new SyntaxErrorException(err);
            }

            if (!AreTypesCompatible(left.Type, right.Type))
            {
                err = ErrorFormatter.Create(
                    context.Span.ToString(),
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
        private static SQLExpressionMetadata AnalyzeArithmetic(BinaryExpression binary, ref SQLValidatorContext context)
        {
            var left = AnalyzeExpression(binary.Left, ref context);
            var right = AnalyzeExpression(binary.Right, ref context);
            var op = binary.Operator;

            string err;

            if (left.Type == SQLExpressionType.Unknown ||
                right.Type == SQLExpressionType.Unknown)
            {
                err = ErrorFormatter.Create(
                    context.Span.ToString(),
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
                        context.Span.ToString(),
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
                        context.Span.ToString(),
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
                    context.Span.ToString(),
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
        private static SQLExpressionMetadata AnalyzeLiteral(LiteralExpression literal, ref SQLValidatorContext context)
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
                context.Span.ToString(),
                literal.Span.Start,
                $"Unexpected literal Type '{literal.Value}' ({literal.Type})",
                literal.Span.Length);

            throw new SyntaxErrorException(err);
        }
        private static SQLExpressionMetadata AnalyzeWildcard(WildcardExpression wildcard, ref SQLValidatorContext context)
        {
            if (wildcard.TableAlias != null)
            {
                if (wildcard.TableAlias.Count > 1)
                {
                    var err = ErrorFormatter.Create(
                        context.Span.ToString(),
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
        private static SQLExpressionMetadata AnalyzeColumn(ColumnExpression column, ref SQLValidatorContext context)
        {
            if (column.Table == null)
            {
                var err = ErrorFormatter.Create(
                    context.Span.ToString(),
                    column.Span.Start,
                    $"Column must be a qualified column name!",
                    column.Span.Length);

                throw new SyntaxErrorException(err);
            }

            if (column.Table.Count > 1)
            {
                var err = ErrorFormatter.Create(
                    context.Span.ToString(),
                    column.Span.Start,
                    $"Invalid column reference '{string.Join(".", column.Table)}.{column.Column}'. " +
                    "Only a single table alias is supported.",
                    column.Span.Length);

                throw new SyntaxErrorException(err);
            }

            var metadata = context.TableManager.GetColumnMetadata(context.Span.ToString(), column);

            return new SQLExpressionMetadata
            {
                IsConstant = false,
                IsNullable = metadata.IsNullable,
                Type = metadata.ExpressionType,
            };
        }
        #endregion
        private static void RegisterSelectOutput(SelectExpression select, SQLExpressionMetadata metadata, ref SQLValidatorContext context)
        {
            if (select.Alias != null)
            {
                context.DtoManager.RegisterOutput(select.Alias.Value, metadata);
                return;
            }

            if (select.Expression is ColumnExpression column)
            {
                context.DtoManager.RegisterOutput(column.Column.Value);
                return;
            }

            if (select.Expression is WildcardExpression wildcard)
            {
                if (wildcard.TableAlias != null)
                {
                    var columns = context.TableManager.GetTableColumns(context.Span.ToString(), wildcard);

                    foreach (var c in columns)
                        context.DtoManager.RegisterOutput(c.Key);
                }
                else
                {
                    var tables = context.TableManager.Tables;

                    foreach (var table in tables.Values)
                    {
                        foreach (var c in table.Columns)
                            context.DtoManager.RegisterOutput(c.Key);
                    }
                }
                return;
            }
            var err = ErrorFormatter.Create(
                context.Span.ToString(),
                select.Span.Start,
                $"A non-column SELECT expression must have an alias.",
                select.Span.Length);
            throw new SyntaxErrorException(err);
        }
        private static bool AreTypesCompatible(SQLExpressionType left, SQLExpressionType right)
        {
            if (left == SQLExpressionType.NullLiteral ||
                right == SQLExpressionType.NullLiteral)
                return true;

            if (SQLDictionary.NumericTypes.Contains(left) &&
                SQLDictionary.NumericTypes.Contains(right))
                return true;

            return left == right;
        }
    }
}

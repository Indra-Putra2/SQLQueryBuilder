using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.SQL_Expression;
using SQLQueryBuilder.SQLEnum;
using System.Data;
using System.Diagnostics;
using System.Reflection;

namespace SQLQueryBuilder.Builder.SQLParser
{
    public class SQLSemanticValidator
    {
        private Dictionary<string, Type> _aliases = new(StringComparer.InvariantCultureIgnoreCase);
        private SQLFunctionDefinition _sQLFunctionDefinitions = new SQLFunctionDefinition();
        private SQLValidationMode _mode;

        public SQLSemanticValidator(SQLValidationMode mode)
        {
            _mode = mode;
        }

        #region Validation
        public void ValidateQuery(SQLQuery query)
        {
            ValidateBaseQuery(query);
            ValidateFilter(query);
        }

        public void ValidateBaseQuery(SQLQuery query)
        {
            ValidateFrom(query.From);
            ValidateJoins(query.Joins);
            ValidateSelect(query.Select);
        }

        public void ValidateFilter(SQLQuery query)
        {
            ValidateHaving(query.Having);
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

            AddAlias(from.Alias, from.Type);
        }
        private void ValidateSelect(List<SelectExpression> select)
        {
            foreach (var selected in select)
            {
                AnalyzeExpression(selected);
            }
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

                AddAlias(table.Alias, table.Type);
                AnalyzeExpression(join.Condition);
            }
        }

        private void ValidateWhere(SQLExpression? where)
        {
            if (where != null)
                AnalyzeExpression(where);
        }

        private void ValidateHaving(SQLExpression? having)
        {
            if (having != null)
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
                SelectExpression select => AnalyzeExpression(select.Expression),
                OrderByExpression orderBy => AnalyzeExpression(orderBy.Expression),

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

                _ => throw new NotSupportedException($"Expression '{expression.GetType().Name}' is not supported.")
            };
        }
        private SQLExpressionMetadata AnalyzeFunction(FunctionExpression function)
        {
            var returnType =
                _sQLFunctionDefinitions.GetFunctionExpressionType(function, _mode);

            if (returnType.type == SQLExpressionType.NullLiteral)
            {
                return new SQLExpressionMetadata
                {
                    Type = SQLExpressionType.NullLiteral,
                    IsNullable = true,
                    IsConstant = true,
                };
            }

            bool isNullable = returnType.rule switch
            {
                SQLNullabilityRule.Propagate => false,
                SQLNullabilityRule.AllNullable => true,
                SQLNullabilityRule.Never => false,
                SQLNullabilityRule.Unknown => true,
                _ => throw new ArgumentOutOfRangeException()
            };

            bool isConstant = returnType.rule != SQLNullabilityRule.Unknown;

            foreach (var arg in function.Arguments)
            {
                var metadata = AnalyzeExpression(arg);

                isConstant &= metadata.IsConstant;

                switch (returnType.rule)
                {
                    case SQLNullabilityRule.Propagate:
                        isNullable |= metadata.IsNullable;
                        break;

                    case SQLNullabilityRule.AllNullable:
                        isNullable &= metadata.IsNullable;
                        break;

                    case SQLNullabilityRule.Never:
                    case SQLNullabilityRule.Unknown:
                        break;
                }
            }

            return new SQLExpressionMetadata
            {
                Type = returnType.type,
                IsNullable = isNullable,
                IsConstant = isConstant,
            };
        }
        private SQLExpressionMetadata AnalyzeUnary(UnaryExpression unary)
        {
            var operand = AnalyzeExpression(unary.Operand);

            // 1. Early Null Propagation
            if (operand.Type == SQLExpressionType.NullLiteral)
            {
                return operand;
            }

            switch (unary.Operator)
            {
                case SQLUnaryOperator.Positive:
                case SQLUnaryOperator.Negative:
                    // CRASH: If we are strict and don't know the type, 
                    // OR if we do know it and it's completely wrong (not a number).
                    if ((_mode == SQLValidationMode.Strict && operand.Type == SQLExpressionType.Unknown) ||
                        (operand.Type != SQLExpressionType.Number && operand.Type != SQLExpressionType.Unknown))
                    {
                        throw new SyntaxErrorException($"Unary operator '{unary.Operator}' cannot be applied to type {operand.Type}.");
                    }

                    // LENIENT MODE INFERENCE: If it was Unknown, it *must* behave like a Number here.
                    if (operand.Type == SQLExpressionType.Unknown)
                    {
                        return operand with { Type = SQLExpressionType.Number };
                    }
                    return operand;

                case SQLUnaryOperator.NOT:
                    // CRASH: Same rules apply for booleans.
                    if ((_mode == SQLValidationMode.Strict && operand.Type == SQLExpressionType.Unknown) ||
                        (operand.Type != SQLExpressionType.Boolean && operand.Type != SQLExpressionType.Unknown))
                    {
                        throw new SyntaxErrorException($"Logical NOT operator cannot be applied to type {operand.Type}.");
                    }

                    // BOTH MODES: Whether it was Boolean or Unknown, the output of NOT is always a Boolean.
                    return operand with { Type = SQLExpressionType.Boolean };

                default:
                    throw new SyntaxErrorException($"Unsupported unary operator '{unary.Operator}'.");
            }
        }
        private SQLExpressionMetadata AnalyzeIsNull(IsNullExpression isn)
        {
            var operandMeta = AnalyzeExpression(isn.Operand);

            // Enforce Validation Mode rules for Unknown types
            if (operandMeta.Type == SQLExpressionType.Unknown && _mode == SQLValidationMode.Strict)
            {
                throw new SyntaxErrorException("Cannot determine expression type for IS NULL check in Strict mode.");
            }

            return new SQLExpressionMetadata
            {
                // IS NULL always returns true or false, NEVER a third null state itself!
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

            // 1. Strict Mode Unknown Check
            if (_mode == SQLValidationMode.Strict &&
               (targetMeta.Type == SQLExpressionType.Unknown ||
                lowMeta.Type == SQLExpressionType.Unknown ||
                highMeta.Type == SQLExpressionType.Unknown))
            {
                throw new SyntaxErrorException("Cannot determine expression types for BETWEEN operator in Strict mode.");
            }

            // 2. Type Unification Check: Target, Low, and High must be compatible!
            // (Ignoring NullLiterals and Unknowns which fit anywhere)
            SQLExpressionType? knownType = null;

            foreach (var type in new[] { targetMeta.Type, lowMeta.Type, highMeta.Type })
            {
                if (type == SQLExpressionType.NullLiteral ||
                    type == SQLExpressionType.Unknown)
                    continue;

                if (knownType == null)
                {
                    knownType = type;
                    continue;
                }

                if (knownType != type)
                {
                    throw new SyntaxErrorException(
                        $"Type mismatch in BETWEEN expression. Cannot compare target of type '{targetMeta.Type}' " +
                        $"against bounds of types '{lowMeta.Type}' and '{highMeta.Type}'.");
                }
            }
            // 3. Propagation Logic
            bool combinedNullable = targetMeta.IsNullable || lowMeta.IsNullable || highMeta.IsNullable ||
                                     targetMeta.Type == SQLExpressionType.NullLiteral ||
                                     lowMeta.Type == SQLExpressionType.NullLiteral ||
                                     highMeta.Type == SQLExpressionType.NullLiteral;

            bool combinedConstant = targetMeta.IsConstant && lowMeta.IsConstant && highMeta.IsConstant;

            return new SQLExpressionMetadata
            {
                Type = SQLExpressionType.Boolean,
                IsNullable = combinedNullable,
                IsConstant = combinedConstant
            };

            bool NullOrUnknown(SQLExpressionType targetMeta) => targetMeta == SQLExpressionType.NullLiteral || targetMeta == SQLExpressionType.Unknown;
        }
        private SQLExpressionMetadata AnalyzeIn(InExpression @in)
        {
            var targetMeta = AnalyzeExpression(@in.Operand);

            // 1. Strict Mode Unknown Check
            if (_mode == SQLValidationMode.Strict && targetMeta.Type == SQLExpressionType.Unknown)
            {
                throw new SyntaxErrorException("Cannot determine target expression type for IN operator in Strict mode.");
            }

            // Establish our anchor type if the target is known
            SQLExpressionType? knownType = (targetMeta.Type != SQLExpressionType.NullLiteral && targetMeta.Type != SQLExpressionType.Unknown)
                ? targetMeta.Type
                : null;

            bool itemsAreNullable = false;
            bool itemsAreConstant = true;

            // 2. Loop through the IN item list
            foreach (var itemExpr in @in.Values)
            {
                var itemMeta = AnalyzeExpression(itemExpr);

                // Track propagation properties
                if (itemMeta.IsNullable || itemMeta.Type == SQLExpressionType.NullLiteral) itemsAreNullable = true;
                if (!itemMeta.IsConstant) itemsAreConstant = false;

                if (itemMeta.Type == SQLExpressionType.NullLiteral || itemMeta.Type == SQLExpressionType.Unknown)
                    continue;

                // If target was unknown but we hit a known item type, lock it in as our benchmark
                if (knownType == null)
                {
                    knownType = itemMeta.Type;
                    continue;
                }

                // Catch type conflicts between the target or other items
                if (knownType != itemMeta.Type)
                {
                    throw new SyntaxErrorException(
                        $"Type mismatch in IN list expression. Found conflicting type '{itemMeta.Type}' " +
                        $"against expected type '{knownType}'.");
                }
            }

            // 3. Final Metadata Unification
            bool combinedNullable = targetMeta.IsNullable || targetMeta.Type == SQLExpressionType.NullLiteral || itemsAreNullable;
            bool combinedConstant = targetMeta.IsConstant && itemsAreConstant;

            return new SQLExpressionMetadata
            {
                Type = SQLExpressionType.Boolean,
                IsNullable = combinedNullable,
                IsConstant = combinedConstant
            };
        }
        private SQLExpressionMetadata AnalyzeLike(LikeExpression like)
        {
            var leftMeta = AnalyzeExpression(like.Operand);
            var rightMeta = AnalyzeExpression(like.Pattern);

            // 1. Strict Mode Unknown Check
            if (_mode == SQLValidationMode.Strict &&
               (leftMeta.Type == SQLExpressionType.Unknown || rightMeta.Type == SQLExpressionType.Unknown))
            {
                throw new SyntaxErrorException("Cannot determine expression types for LIKE operator in Strict mode.");
            }

            // 2. Strict Type Constraints (Both sides MUST resolve to String)
            if (leftMeta.Type != SQLExpressionType.String && leftMeta.Type != SQLExpressionType.NullLiteral && leftMeta.Type != SQLExpressionType.Unknown)
            {
                throw new SyntaxErrorException($"The left side of a LIKE expression must be a String, but found '{leftMeta.Type}'.");
            }

            if (rightMeta.Type != SQLExpressionType.String && rightMeta.Type != SQLExpressionType.NullLiteral && rightMeta.Type != SQLExpressionType.Unknown)
            {
                throw new SyntaxErrorException($"The LIKE pattern pattern must be a String, but found '{rightMeta.Type}'.");
            }

            // 3. Propagation Logic
            bool combinedNullable = leftMeta.IsNullable || rightMeta.IsNullable ||
                                     leftMeta.Type == SQLExpressionType.NullLiteral ||
                                     rightMeta.Type == SQLExpressionType.NullLiteral;

            bool combinedConstant = leftMeta.IsConstant && rightMeta.IsConstant;

            return new SQLExpressionMetadata
            {
                Type = SQLExpressionType.Boolean,
                IsNullable = combinedNullable,
                IsConstant = combinedConstant
            };
        }
        private SQLExpressionMetadata AnalyzeCase(CaseExpression caseExpr)
        {
            SQLExpressionMetadata? baseMeta = caseExpr.BaseExpression != null
                ? AnalyzeExpression(caseExpr.BaseExpression)
                : null;

            SQLExpressionType expectedCaseType = baseMeta == null ? SQLExpressionType.Unknown : baseMeta.Type;
            SQLExpressionType expectedReturnType = SQLExpressionType.Unknown;
            bool combinedNullable = false;
            bool combinedConstant = baseMeta?.IsConstant ?? true;

            // If there's no ELSE branch, the result is implicitly nullable!
            if (caseExpr.ElseExpression == null)
            {
                combinedNullable = true;
            }

            // 1. Validate all WHEN / THEN branches
            foreach (var branch in caseExpr.Conditions)
            {
                var whenMeta = AnalyzeExpression(branch.Condition);
                var thenMeta = AnalyzeExpression(branch.Result);

                combinedConstant &= whenMeta.IsConstant && thenMeta.IsConstant;
                combinedNullable |= thenMeta.IsNullable || thenMeta.Type == SQLExpressionType.NullLiteral;

                // Validate the WHEN clause condition
                if (baseMeta != null)
                {
                    if (_mode == SQLValidationMode.Strict && expectedCaseType == SQLExpressionType.Unknown)
                        throw new SyntaxErrorException("Cannot Determine Base Expression Type!");

                    expectedCaseType = UnifyCaseTypes(expectedCaseType, whenMeta.Type);

                    if (expectedCaseType == SQLExpressionType.Unknown && _mode == SQLValidationMode.Infer)
                        throw new SyntaxErrorException("Cannot Determine Base Expression Type!");
                }
                else
                {
                    // Searched CASE: WHEN must evaluate to a Boolean
                    if (whenMeta.Type != SQLExpressionType.Boolean && whenMeta.Type != SQLExpressionType.Unknown)
                    {
                        throw new SyntaxErrorException($"CASE WHEN condition must evaluate to a Boolean, not {whenMeta.Type}.");
                    }
                }

                // Track and unify the THEN return type
                expectedReturnType = UnifyCaseTypes(expectedReturnType, thenMeta.Type);
            }

            // 2. Validate the optional ELSE branch
            if (caseExpr.ElseExpression != null)
            {
                var elseMeta = AnalyzeExpression(caseExpr.ElseExpression);
                combinedConstant &= elseMeta.IsConstant;
                combinedNullable |= elseMeta.IsNullable || elseMeta.Type == SQLExpressionType.NullLiteral;

                expectedReturnType = UnifyCaseTypes(expectedReturnType, elseMeta.Type);
            }

            // 3. Enforce Validation Modes on the final unified type
            if (expectedReturnType == SQLExpressionType.Unknown)
            {
                if (_mode == SQLValidationMode.Strict)
                {
                    throw new SyntaxErrorException("Cannot determine return type of CASE statement in Strict mode.");
                }
                if (_mode == SQLValidationMode.Infer)
                {
                    throw new SyntaxErrorException("Cannot determine return type of CASE statement when all branches are Unknown in Infer mode.");
                }
            }

            return new SQLExpressionMetadata
            {
                Type = expectedReturnType,
                IsNullable = combinedNullable,
                IsConstant = combinedConstant
            };

            SQLExpressionType UnifyCaseTypes(SQLExpressionType current, SQLExpressionType branchType)
            {
                return (current, branchType) switch
                {
                    // If we don't know the type yet, adopt the branch type (unless it's Null/Unknown)
                    (SQLExpressionType.Unknown, SQLExpressionType.NullLiteral) => SQLExpressionType.Unknown,
                    (SQLExpressionType.Unknown, _) => branchType,

                    // If the current type is locked in, ensure the incoming branch type is compatible
                    (_, SQLExpressionType.Unknown) => current,
                    (_, SQLExpressionType.NullLiteral) => current,

                    // Exact matches
                    (SQLExpressionType.Number, SQLExpressionType.Number) => SQLExpressionType.Number,
                    (SQLExpressionType.String, SQLExpressionType.String) => SQLExpressionType.String,
                    (SQLExpressionType.Boolean, SQLExpressionType.Boolean) => SQLExpressionType.Boolean,

                    // Explicit Type Mismatch!
                    _ => throw new SyntaxErrorException($"Mismatched return types in CASE branches: cannot unify {current} and {branchType}.")
                };
            }
        }
        private SQLExpressionMetadata AnalyzeBinary(BinaryExpression binary)
        {
            var left = AnalyzeExpression(binary.Left);
            var op = binary.Operator;
            var right = AnalyzeExpression(binary.Right);

            if (IsArithmeticOperator(op))
                return AnalyzeArithmetic(left, op, right);
            else if (IsComparisonOperator(op))
                return AnalyzeComparison(left, op, right);
            else if (IsLogicalOperator(op))
                return AnalyzeLogical(left, op, right);
            else if (IsBitwiseOperator(op))
                return AnalyzeBitwise(left, op, right);

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
        private SQLExpressionMetadata AnalyzeBitwise(SQLExpressionMetadata left, SQLBinaryOperator op, SQLExpressionMetadata right)
        {
            bool combinedNullable = left.IsNullable || right.IsNullable ||
                                     left.Type == SQLExpressionType.NullLiteral ||
                                     right.Type == SQLExpressionType.NullLiteral;

            bool combinedConstant = left.IsConstant && right.IsConstant;

            // 1. Strict Mode Safeguard
            if (_mode == SQLValidationMode.Strict && (left.Type == SQLExpressionType.Unknown || right.Type == SQLExpressionType.Unknown))
            {
                throw new SyntaxErrorException($"Cannot determine operand types for bitwise operator '{op}' in Strict mode.");
            }

            SQLExpressionType returnType;

            returnType = (left.Type, right.Type) switch
            {
                // 2. Pure Matching Integers/Numbers -> Returns Number
                (SQLExpressionType.Number, SQLExpressionType.Number) => SQLExpressionType.Number,

                // 3. SQL Null Handling -> Returns Number (Nullable)
                (SQLExpressionType.Number, SQLExpressionType.NullLiteral) => SQLExpressionType.Number,
                (SQLExpressionType.NullLiteral, SQLExpressionType.Number) => SQLExpressionType.Number,
                (SQLExpressionType.NullLiteral, SQLExpressionType.NullLiteral) => SQLExpressionType.NullLiteral,

                // 4. Unknown Inference Cases
                (SQLExpressionType.Unknown, SQLExpressionType.Unknown) => SQLExpressionType.Unknown,
                (SQLExpressionType.Unknown, SQLExpressionType.NullLiteral) => SQLExpressionType.Unknown,
                (SQLExpressionType.NullLiteral, SQLExpressionType.Unknown) => SQLExpressionType.Unknown,

                // If one side is Unknown and the other is a Number, infer it to match -> Number
                (SQLExpressionType.Unknown, SQLExpressionType.Number) => SQLExpressionType.Number,
                (SQLExpressionType.Number, SQLExpressionType.Unknown) => SQLExpressionType.Number,

                // 5. Explicit Mismatch (e.g., String & Number, Boolean | Number) -> Error!
                _ => throw new SyntaxErrorException($"Bitwise operator '{op}' can only be applied to integer/numeric expressions. Got {left.Type} and {right.Type}.")
            };

            // 6. Enforce Mode Rules for double Unknown states
            if (returnType == SQLExpressionType.Unknown && _mode == SQLValidationMode.Infer)
            {
                throw new SyntaxErrorException($"Cannot determine operand types for bitwise operator '{op}' when both sides are Unknown in Infer mode.");
            }

            return left with
            {
                Type = returnType,
                IsNullable = combinedNullable,
                IsConstant = combinedConstant
            };
        }
        private SQLExpressionMetadata AnalyzeLogical(SQLExpressionMetadata left, SQLBinaryOperator op, SQLExpressionMetadata right)
        {
            bool combinedNullable = left.IsNullable || right.IsNullable ||
                                     left.Type == SQLExpressionType.NullLiteral ||
                                     right.Type == SQLExpressionType.NullLiteral;

            bool combinedConstant = left.IsConstant && right.IsConstant;

            // 1. Strict Mode Safeguard
            if (_mode == SQLValidationMode.Strict && (left.Type == SQLExpressionType.Unknown || right.Type == SQLExpressionType.Unknown))
            {
                throw new SyntaxErrorException($"Cannot determine operand types for logical operator '{op}' in Strict mode.");
            }

            SQLExpressionType returnType;

            returnType = (left.Type, right.Type) switch
            {
                // 2. Pure Matching Types -> Returns Boolean
                (SQLExpressionType.Boolean, SQLExpressionType.Boolean) => SQLExpressionType.Boolean,

                // 3. SQL Null Handling for Logical (Three-Valued Logic) -> Returns Boolean (Nullable)
                (SQLExpressionType.Boolean, SQLExpressionType.NullLiteral) => SQLExpressionType.Boolean,
                (SQLExpressionType.NullLiteral, SQLExpressionType.Boolean) => SQLExpressionType.Boolean,
                (SQLExpressionType.NullLiteral, SQLExpressionType.NullLiteral) => SQLExpressionType.Boolean,

                // 4. Unknown Inference Cases
                (SQLExpressionType.Unknown, SQLExpressionType.Unknown) => SQLExpressionType.Unknown,
                (SQLExpressionType.Unknown, SQLExpressionType.NullLiteral) => SQLExpressionType.Unknown,
                (SQLExpressionType.NullLiteral, SQLExpressionType.Unknown) => SQLExpressionType.Unknown,

                // If one side is Unknown and the other is a Boolean, infer it to match -> Boolean
                (SQLExpressionType.Unknown, SQLExpressionType.Boolean) => SQLExpressionType.Boolean,
                (SQLExpressionType.Boolean, SQLExpressionType.Unknown) => SQLExpressionType.Boolean,

                // 5. Explicit Mismatch (e.g., Number AND Boolean) -> Error!
                _ => throw new SyntaxErrorException($"Logical operator '{op}' can only be applied to Boolean expressions. Got {left.Type} and {right.Type}.")
            };

            // 5. Enforce Mode Rules for double Unknown states
            if (returnType == SQLExpressionType.Unknown && _mode == SQLValidationMode.Infer)
            {
                throw new SyntaxErrorException($"Cannot determine operand types for logical operator '{op}' when both sides are Unknown in Infer mode.");
            }

            return left with
            {
                Type = returnType,
                IsNullable = combinedNullable,
                IsConstant = combinedConstant
            };
        }
        private SQLExpressionMetadata AnalyzeComparison(SQLExpressionMetadata left, SQLBinaryOperator op, SQLExpressionMetadata right)
        {
            bool combinedNullable = left.IsNullable || right.IsNullable ||
                                     left.Type == SQLExpressionType.NullLiteral ||
                                     right.Type == SQLExpressionType.NullLiteral;

            bool combinedConstant = left.IsConstant && right.IsConstant;

            // 1. Strict Mode Safeguard: Unknown is illegal in strict mode
            if (_mode == SQLValidationMode.Strict && (left.Type == SQLExpressionType.Unknown || right.Type == SQLExpressionType.Unknown))
            {
                throw new SyntaxErrorException($"Cannot determine operand types for comparison operator '{op}' in Strict mode.");
            }

            SQLExpressionType returnType;

            returnType = (left.Type, right.Type) switch
            {
                // 2. Pure Matching Types Returns Boolean
                (SQLExpressionType.Number, SQLExpressionType.Number) => SQLExpressionType.Boolean,
                (SQLExpressionType.String, SQLExpressionType.String) => SQLExpressionType.Boolean,
                (SQLExpressionType.Boolean, SQLExpressionType.Boolean) => SQLExpressionType.Boolean,
                (SQLExpressionType.DateTime, SQLExpressionType.DateTime) => SQLExpressionType.Boolean,

                // 3. SQL Null Handling for Comparison -> Always produces a Boolean (Nullable)
                (SQLExpressionType.Number, SQLExpressionType.NullLiteral) => SQLExpressionType.Boolean,
                (SQLExpressionType.NullLiteral, SQLExpressionType.Number) => SQLExpressionType.Boolean,
                (SQLExpressionType.String, SQLExpressionType.NullLiteral) => SQLExpressionType.Boolean,
                (SQLExpressionType.NullLiteral, SQLExpressionType.String) => SQLExpressionType.Boolean,
                (SQLExpressionType.Boolean, SQLExpressionType.NullLiteral) => SQLExpressionType.Boolean,
                (SQLExpressionType.NullLiteral, SQLExpressionType.Boolean) => SQLExpressionType.Boolean,
                (SQLExpressionType.NullLiteral, SQLExpressionType.NullLiteral) => SQLExpressionType.Boolean,

                // 4. Unknown Inference Cases
                (SQLExpressionType.Unknown, SQLExpressionType.Unknown) => SQLExpressionType.Unknown,
                (SQLExpressionType.Unknown, SQLExpressionType.NullLiteral) => SQLExpressionType.Unknown,
                (SQLExpressionType.NullLiteral, SQLExpressionType.Unknown) => SQLExpressionType.Unknown,

                // If one side is Unknown and the other is a known type, infer valid comparison -> Boolean
                (SQLExpressionType.Unknown, _) => SQLExpressionType.Boolean,
                (_, SQLExpressionType.Unknown) => SQLExpressionType.Boolean,

                // 5. Explicit Mismatch (e.g., String = Number) -> Error!
                _ => throw new SyntaxErrorException($"Comparison operator '{op}' cannot compare mismatched types {left.Type} and {right.Type}.")
            };

            // 5. Enforce Mode Rules for double Unknown states
            if (returnType == SQLExpressionType.Unknown && _mode == SQLValidationMode.Infer)
            {
                throw new SyntaxErrorException($"Cannot determine operand types for comparison operator '{op}' when both sides are Unknown in Infer mode.");
            }

            return left with
            {
                Type = returnType,
                IsNullable = combinedNullable,
                IsConstant = combinedConstant
            };
        }
        private SQLExpressionMetadata AnalyzeArithmetic(SQLExpressionMetadata left, SQLBinaryOperator op, SQLExpressionMetadata right)
        {
            bool combinedNullable = left.IsNullable || right.IsNullable;
            bool combinedConstant = left.IsConstant && right.IsConstant;

            // 1. Strict Mode Safeguard: Unknown is instantly illegal
            if (_mode == SQLValidationMode.Strict && (left.Type == SQLExpressionType.Unknown || right.Type == SQLExpressionType.Unknown))
            {
                throw new SyntaxErrorException($"Cannot determine operand types for arithmetic operator '{op}' in Strict mode.");
            }

            // 2. Early Null Propagation
            if (left.Type == SQLExpressionType.NullLiteral && right.Type != SQLExpressionType.NullLiteral)
            {
                // 'NULL + 5' is a Number (that evaluates to null)
                return right with
                {
                    IsNullable = true,
                    IsConstant = combinedConstant
                };
            }

            if (right.Type == SQLExpressionType.NullLiteral && left.Type != SQLExpressionType.NullLiteral)
            {
                // ''s' + NULL' is a String (that evaluates to null)
                return left with
                {
                    IsNullable = true,
                    IsConstant = combinedConstant
                };
            }

            if (left.Type == SQLExpressionType.NullLiteral && right.Type == SQLExpressionType.NullLiteral)
            {
                // 'NULL + NULL' stays a NullLiteral
                return new SQLExpressionMetadata
                {
                    Type = SQLExpressionType.NullLiteral,
                    IsNullable = true,
                    IsConstant = true
                };
            }

            SQLExpressionType returnType;

            // 3. Handle Addition (The Overloaded String/Math Operator)
            if (op == SQLBinaryOperator.Add)
            {
                // Fail early if mixing String and Number explicitly
                if ((left.Type == SQLExpressionType.String && right.Type == SQLExpressionType.Number) ||
                    (left.Type == SQLExpressionType.Number && right.Type == SQLExpressionType.String))
                {
                    throw new SyntaxErrorException($"Cannot apply operator '+' between {left.Type} and {right.Type}. Use CAST to convert explicit types.");
                }

                returnType = (left.Type, right.Type) switch
                {
                    (SQLExpressionType.String, SQLExpressionType.String) => SQLExpressionType.String,
                    (SQLExpressionType.Number, SQLExpressionType.Number) => SQLExpressionType.Number,

                    // Unknown inference cases
                    // String
                    (SQLExpressionType.Unknown, SQLExpressionType.String) => SQLExpressionType.String,
                    (SQLExpressionType.String, SQLExpressionType.Unknown) => SQLExpressionType.String,
                    // Number
                    (SQLExpressionType.Unknown, SQLExpressionType.Number) => SQLExpressionType.Number,
                    (SQLExpressionType.Number, SQLExpressionType.Unknown) => SQLExpressionType.Number,
                    (SQLExpressionType.Unknown, SQLExpressionType.Unknown) => SQLExpressionType.Unknown,

                    _ => throw new SyntaxErrorException($"Invalid types for '+' operator: {left.Type} and {right.Type}.")
                };
            }
            // 4. Handle Standard Math Operations (-, *, /, %)
            else
            {
                returnType = (left.Type, right.Type) switch
                {
                    (SQLExpressionType.Number, SQLExpressionType.Number) => SQLExpressionType.Number,

                    // Unknown inference cases
                    (SQLExpressionType.Unknown, SQLExpressionType.Number) => SQLExpressionType.Number,
                    (SQLExpressionType.Number, SQLExpressionType.Unknown) => SQLExpressionType.Number,
                    (SQLExpressionType.Unknown, SQLExpressionType.Unknown) => SQLExpressionType.Unknown,

                    _ => throw new SyntaxErrorException($"Arithmetic operator '{op}' cannot be applied to types {left.Type} and {right.Type}.")
                };
            }

            // 5. Enforce Mode Rules for double Unknown states (Unknown + Unknown)
            if (returnType == SQLExpressionType.Unknown)
            {
                if (_mode == SQLValidationMode.Infer)
                {
                    throw new SyntaxErrorException($"Cannot determine operand types for arithmetic operator '{op}' when both sides are Unknown in Infer mode.");
                }

                // If mode is Lenient, it peacefully falls through here returning Unknown!
            }

            return left with
            {
                Type = returnType,
                IsNullable = combinedNullable,
                IsConstant = combinedConstant
            };
        }
        private SQLExpressionMetadata AnalyzeLiteral(LiteralExpression literal)
        {
            if (literal.Type == SQLLiteralType.UnicodeString)
            {
                return new SQLExpressionMetadata
                {
                    Type = SQLExpressionType.String,
                    IsConstant = true,
                    IsNullable = false,
                };
            }
            else if (literal.Type == SQLLiteralType.Datetime)
            {
                return new SQLExpressionMetadata
                {
                    Type = SQLExpressionType.DateTime,
                    IsConstant = true,
                    IsNullable = false
                };
            }
            else if (literal.Type == SQLLiteralType.String)
            {
                return new SQLExpressionMetadata
                {
                    Type = SQLExpressionType.String,
                    IsConstant = true,
                    IsNullable = false,
                };
            }
            else if (literal.Type == SQLLiteralType.Integer)
            {
                return new SQLExpressionMetadata
                {
                    Type = SQLExpressionType.Number,
                    IsConstant = true,
                    IsNullable = false,
                };
            }
            else if (literal.Type == SQLLiteralType.Scientific)
            {
                return new SQLExpressionMetadata
                {
                    Type = SQLExpressionType.Number,
                    IsConstant = true,
                    IsNullable = false,
                };
            }
            else if (literal.Type == SQLLiteralType.Decimal)
            {
                return new SQLExpressionMetadata
                {
                    Type = SQLExpressionType.Number,
                    IsConstant = true,
                    IsNullable = false,
                };
            }
            else if (literal.Type == SQLLiteralType.BinaryLiteral)
            {
                return new SQLExpressionMetadata
                {
                    Type = SQLExpressionType.Binary,
                    IsConstant = true,
                    IsNullable = false,
                };
            }
            else if (literal.Type == SQLLiteralType.Money)
            {
                return new SQLExpressionMetadata
                {
                    Type = SQLExpressionType.Number,
                    IsConstant = true,
                    IsNullable = false,
                };
            }
            else if (literal.Type == SQLLiteralType.Null)
            {
                return new SQLExpressionMetadata
                {
                    Type = SQLExpressionType.NullLiteral,
                    IsConstant = true,
                    IsNullable = true,
                };
            }

            throw new SyntaxErrorException($"Unexpected literal Type '{literal.Value}' ({literal.Type})");
        }
        private SQLExpressionMetadata AnalyzeWildcard(WildcardExpression wildcard)
        {
            if (wildcard.TableAlias != null)
            {
                if (wildcard.TableAlias.Count > 1)
                {
                    throw new SyntaxErrorException(
                        $"Invalid column reference '{string.Join(".", wildcard.TableAlias)}.*'. " +
                        "Only a single table alias is supported.");
                }

                var type = ResolveAlias(wildcard.TableAlias[0]);

                return new SQLExpressionMetadata
                {
                    Type = SQLExpressionType.Unknown,
                    IsWildcard = true,
                    IsNullable = false,
                    IsConstant = false
                };
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
                throw new ArgumentNullException($"must provide alias for resolving alias!");

            if (column.Table.Count > 1)
            {
                throw new SyntaxErrorException(
                    $"Invalid column reference '{string.Join(".", column.Table)}.{column.Column}'. " +
                    "Only a single table alias is supported.");
            }

            var type = ResolveAlias(column.Table[0]);
            var prop = type.GetProperty(column.Column, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (prop == null)
                throw new SyntaxErrorException($"column with the name '{column.Column}' cannot be found!");

            var mappedType = MapType(prop.PropertyType);

            return new SQLExpressionMetadata
            {
                IsConstant = false,
                IsNullable = IsNullable(prop),
                Type = mappedType,
            };
        }
        #endregion

        #region Helper
        private void AddAlias(string alias, Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (string.IsNullOrWhiteSpace(alias))
                throw new ArgumentNullException($"Table alias for type {type.FullName} cannot be empty or null! ");

            if (_aliases.ContainsKey(alias))
            {
                throw new InvalidOperationException($"Table alias '{alias}' already exists!");
            }

            _aliases[alias] = type;
        }
        private Type ResolveAlias(string alias)
        {
            if (string.IsNullOrWhiteSpace(alias))
                throw new ArgumentNullException($"must provide alias for resolving alias!");

            if (_aliases.TryGetValue(alias, out var type))
            {
                return type;
            }

            throw new InvalidOperationException($"Table alias '{alias}' doesn't exists!");
        }
        private SQLExpressionType MapType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type == typeof(char))
                return SQLExpressionType.String;

            else if (type == typeof(string))
                return SQLExpressionType.String;

            else if (type == typeof(bool))
                return SQLExpressionType.Boolean;

            else if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
                return SQLExpressionType.DateTime;

            else if (type == typeof(Guid))
                return SQLExpressionType.Guid;

            else if (type == typeof(byte[]))
                return SQLExpressionType.Number;

            else if (type == typeof(short))
                return SQLExpressionType.Number;

            else if (type == typeof(int))
                return SQLExpressionType.Number;

            else if (type == typeof(long))
                return SQLExpressionType.Number;

            else if (type == typeof(float))
                return SQLExpressionType.Number;

            else if (type == typeof(double))
                return SQLExpressionType.Number;

            else if (type == typeof(decimal))
                return SQLExpressionType.Number;

            return SQLExpressionType.Unknown;
        }
        private bool IsNullable(PropertyInfo prop)
        {
            var nullabilityContext = new NullabilityInfoContext();
            var nullability = nullabilityContext.Create(prop);
            return nullability.ReadState == NullabilityState.Nullable;
        }
        #endregion

        #region Testing
        public SQLExpressionMetadata DebugAnalyzeExpression(SQLExpression expression)
        {
            return AnalyzeExpression(expression);
        }

        public SQLExpressionMetadata DebugAnalyzeColumnExpression(TableSource tableSource, SQLExpression expression)
        {
            ValidateFrom(tableSource);
            return AnalyzeExpression(expression);
        }

        public SQLExpressionMetadata DebugAnalyzeWildcardExpression(TableSource tableSource, SQLExpression expression)
        {
            ValidateFrom(tableSource);
            return AnalyzeExpression(expression);
        }

        public SQLExpressionMetadata DebugAnalyzeFunctionExpression(TableSource tableSource, SQLExpression expression)
        {
            ValidateFrom(tableSource);
            return AnalyzeExpression(expression);
        }
        public SQLExpressionMetadata DebugAnalyzeCASEExpression(TableSource tableSource, SQLExpression expression)
        {
            ValidateFrom(tableSource);
            return AnalyzeExpression(expression);
        }

        public SQLExpressionMetadata DebugAnalyzeUnaryExpression(TableSource tableSource, SQLExpression expression)
        {
            ValidateFrom(tableSource);
            return AnalyzeExpression(expression);
        }
        public SQLExpressionMetadata DebugAnalyzeBinaryExpression(TableSource tableSource, SQLExpression expression)
        {
            ValidateFrom(tableSource);
            return AnalyzeExpression(expression);
        }

        public (string Alias, Type type) DebugValidateFromSource(TableSource tableSource)
        {
            ValidateFrom(tableSource);
            _aliases.TryGetValue(tableSource.Alias, out var aliasType);
            return (tableSource.Alias, aliasType);
        }
        #endregion
    }
}

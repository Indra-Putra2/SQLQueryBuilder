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
        private SQLFunctionDefinition _sQLFunction = new();
        public void ValidateQuery(SQLQuery query)
        {
            ValidateBaseQuery(query);
            ValidateFilter(query);
        }

        public void ValidateBaseQuery(SQLQuery query)
        {
            ValidateFrom(query.From);
            ValidateSelect(query.Select);
            ValidateJoins(query.Joins);
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
            }
        }
        private void ValidateOrderBy(object orderBy)
        {
            throw new NotImplementedException();
        }

        private void ValidateHaving(object having)
        {
            throw new NotImplementedException();
        }

        private void ValidateGroupBy(object groupBy)
        {
            throw new NotImplementedException();
        }

        private void ValidateWhere(object where)
        {
            throw new NotImplementedException();
        }

        private void ValidateSelect(object select)
        {
            throw new NotImplementedException();
        }

        private ExpressionMetadata AnalyzeExpression(SQLExpression expression)
        {
            return expression switch
            {
                SelectExpression select => AnalyzeExpression(select.Expression),
                OrderByExpression orderBy => AnalyzeExpression(orderBy.Expression),

                BinaryExpression binary => AnalyzeBinary(
                    AnalyzeExpression(binary.Left),
                    binary.Operator,
                    AnalyzeExpression(binary.Right)),

                UnaryExpression unary => AnalyzeUnary(unary),

                FunctionExpression function => AnalyzeFunction(function),
                ColumnExpression column => AnalyzeColumn(column),
                LiteralExpression literal => AnalyzeLiteral(literal),
                WildcardExpression wildcard => AnalyzeWildcard(wildcard),

                CaseExpression @case => AnalyzeCase(@case),
                LikeExpression like => AnalyzeLike(like),
                InExpression @in => AnalyzeIn(@in),
                BetweenExpression between => AnalyzeBetween(between),
                IsNullExpression isnull => AnalyzeIsNull(isnull),

                _ => throw new NotSupportedException(
                    $"Expression '{expression.GetType().Name}' is not supported.")
            };
        }
        private ExpressionMetadata AnalyzeFunction(FunctionExpression function)
        {
            var args = function.Arguments
                .Select(AnalyzeExpression)
                .ToList();

            if (_sQLFunction.Functions.TryGetValue(function.Name, out var definition))
                return definition.Analyzer(args);

            return new ExpressionMetadata
            {
                Type = SQLValueType.Unknown,
                IsNullable = true,
                IsConstant = args.All(x => x.IsConstant)
            };
        }
        private ExpressionMetadata AnalyzeUnary(UnaryExpression unary)
        {
            var operand = AnalyzeExpression(unary.Operand);

            return unary.Operator switch
            {
                SQLUnaryOperator.Positive or SQLUnaryOperator.Negative
                    when operand.Type == SQLValueType.Number
                    => new ExpressionMetadata
                    {
                        Type = SQLValueType.Number,
                        IsNullable = operand.IsNullable,
                        IsConstant = operand.IsConstant
                    },

                SQLUnaryOperator.NOT
                    when operand.Type == SQLValueType.Boolean
                    => new ExpressionMetadata
                    {
                        Type = SQLValueType.Boolean,
                        IsNullable = operand.IsNullable,
                        IsConstant = operand.IsConstant
                    },

                SQLUnaryOperator.BitwiseNOT
                    when operand.Type == SQLValueType.Number
                    => new ExpressionMetadata
                    {
                        Type = SQLValueType.Number,
                        IsNullable = operand.IsNullable,
                        IsConstant = operand.IsConstant
                    },

                _ => throw new SyntaxErrorException(
                    $"Unary operator '{unary.Operator}' cannot be applied to {operand.Type}.")
            };
        }
        private ExpressionMetadata AnalyzeIsNull(IsNullExpression isn)
        {
            var operand = AnalyzeExpression(isn.Operand);

            return new ExpressionMetadata
            {
                Type = SQLValueType.Boolean,
                IsNullable = false,
                IsConstant = operand.IsConstant
            };
        }
        private ExpressionMetadata AnalyzeBetween(BetweenExpression b)
        {
            var operand = AnalyzeExpression(b.Operand);
            var upper = AnalyzeExpression(b.Upper);
            var lower = AnalyzeExpression(b.Lower);

            // 1. Literal NULL propagation
            if (operand.Type == SQLValueType.NullLiteral || upper.Type == SQLValueType.NullLiteral || lower.Type == SQLValueType.NullLiteral)
            {
                return new ExpressionMetadata
                {
                    Type = SQLValueType.Boolean,
                    IsNullable = true,
                    IsConstant = operand.IsConstant
                                 && upper.IsConstant
                                 && lower.IsConstant
                };
            }

            if (operand.Type != lower.Type ||
                operand.Type != upper.Type)
            {
                throw new SyntaxErrorException(
                    "BETWEEN operands must have compatible types.");
            }

            // 3. Return Boolean metadata
            return new ExpressionMetadata
            {
                Type = SQLValueType.Boolean,
                IsNullable = operand.IsNullable || upper.IsNullable || lower.IsNullable,
                IsConstant = operand.IsConstant && upper.IsConstant && lower.IsConstant
            };
        }
        private ExpressionMetadata AnalyzeIn(InExpression i)
        {
            var operand = AnalyzeExpression(i.Operand);
            bool allConstant = true;

            foreach (var v in i.Values)
            {
                var value = AnalyzeExpression(v);
                if (operand.Type != value.Type)
                    throw new SyntaxErrorException(
                        "IN values must have the same type.");
                allConstant &= value.IsConstant;
            }
            allConstant &= operand.IsConstant;

            return new ExpressionMetadata
            {
                Type = SQLValueType.Boolean,
                IsNullable = operand.IsNullable,
                IsConstant = allConstant
            };
        }
        private ExpressionMetadata AnalyzeLike(LikeExpression l)
        {
            var operand = AnalyzeExpression(l.Operand);
            var pattern = AnalyzeExpression(l.Pattern);

            if (operand.Type == SQLValueType.NullLiteral || pattern.Type == SQLValueType.NullLiteral)
                return new ExpressionMetadata
                {
                    Type = SQLValueType.Boolean,
                    IsNullable = true,
                    IsConstant = operand.IsConstant && pattern.IsConstant
                };

            if (operand.Type != SQLValueType.String)
                throw new SyntaxErrorException("Operand Must be string!");

            if (pattern.Type != SQLValueType.String)
                throw new SyntaxErrorException("pattern Must be string!");

            return new ExpressionMetadata
            {
                Type = SQLValueType.Boolean,
                IsNullable = operand.IsNullable || pattern.IsNullable,
                IsConstant = operand.IsConstant && pattern.IsConstant
            };
        }
        private ExpressionMetadata AnalyzeCase(CaseExpression c)
        {
            SQLValueType? targetType = null;

            bool allConstant = true;
            bool anyResultNullable = false;

            foreach (var conMetadata in c.Conditions)
            {
                var condition = AnalyzeExpression(conMetadata.Condition);
                var result = AnalyzeExpression(conMetadata.Result);

                if (condition.Type != SQLValueType.Boolean)
                    throw new SyntaxErrorException("CASE condition must return boolean.");

                if (targetType == null)
                {
                    targetType = result.Type;
                }
                else if (result.Type != targetType)
                {
                    throw new SyntaxErrorException(
                        $"CASE results must have compatible types ({targetType}).");
                }

                allConstant &= condition.IsConstant && result.IsConstant;
                anyResultNullable |= result.IsNullable;
            }

            if (c.ElseExpression != null)
            {
                var elseResult = AnalyzeExpression(c.ElseExpression);

                if (targetType == null)
                {
                    targetType = elseResult.Type;
                }
                else if (elseResult.Type != targetType)
                {
                    throw new SyntaxErrorException(
                        $"CASE ELSE type must match CASE result type ({targetType}).");
                }

                allConstant &= elseResult.IsConstant;
                anyResultNullable |= elseResult.IsNullable;
            }
            else
            {
                anyResultNullable = true;
            }

            return new ExpressionMetadata
            {
                Type = targetType ?? SQLValueType.Unknown,
                IsNullable = anyResultNullable,
                IsConstant = allConstant
            };
        }
        private ExpressionMetadata AnalyzeBinary(ExpressionMetadata left, SQLBinaryOperator op, ExpressionMetadata right)
        {
            if (IsArithmeticOperator(op))
                return AnalyzeArithmetic(left, op, right);
            else if (IsComparisonOperator(op))
                return AnalyzeComparison(left, op, right);
            else if (IsLogicalOperator(op))
                return AnalyzeLogical(left, op, right);
            else if (IsBitwiseOperator(op))
                return AnalyzeBitwise(left, op, right);

            throw new UnreachableException($"Unhandled binary operator: {op}");
        }
        private ExpressionMetadata AnalyzeBitwise(ExpressionMetadata left, SQLBinaryOperator op, ExpressionMetadata right)
        {
            if (left.Type != SQLValueType.Number ||
                right.Type != SQLValueType.Number)
            {
                throw new SyntaxErrorException(
                    $"Operator '{op}' requires numeric operands.");
            }

            return new ExpressionMetadata
            {
                Type = SQLValueType.Number,
                IsNullable = left.IsNullable || right.IsNullable,
                IsConstant = left.IsConstant && right.IsConstant
            };
        }
        private ExpressionMetadata AnalyzeLogical(ExpressionMetadata left, SQLBinaryOperator op, ExpressionMetadata right)
        {
            if (left.Type != SQLValueType.Boolean ||
                right.Type != SQLValueType.Boolean)
            {
                throw new SyntaxErrorException(
                    $"Operator '{op}' requires boolean operands.");
            }

            return new ExpressionMetadata
            {
                Type = SQLValueType.Boolean,
                IsNullable = left.IsNullable || right.IsNullable,
                IsConstant = left.IsConstant && right.IsConstant
            };
        }
        private ExpressionMetadata AnalyzeComparison(ExpressionMetadata left, SQLBinaryOperator op, ExpressionMetadata right)
        {
            if (!AreComparable(left, right))
                throw new SyntaxErrorException(
                    $"Cannot compare {left.Type} and {right.Type}");

            return new ExpressionMetadata
            {
                Type = SQLValueType.Boolean,
                IsNullable = left.IsNullable || right.IsNullable,
                IsConstant = left.IsConstant && right.IsConstant
            };
        }
        private ExpressionMetadata AnalyzeArithmetic(ExpressionMetadata left, SQLBinaryOperator op, ExpressionMetadata right)
        {
            if (op == SQLBinaryOperator.Add)
            {
                return (left.Type, right.Type) switch
                {
                    (SQLValueType.Unknown, SQLValueType.Unknown)
                        => new ExpressionMetadata
                        {
                            Type = SQLValueType.Unknown,
                            IsNullable = true
                        },

                    (SQLValueType.Number, SQLValueType.Number)
                        => new ExpressionMetadata
                        {
                            Type = SQLValueType.Number,
                            IsNullable = left.IsNullable || right.IsNullable,
                            IsConstant = left.IsConstant && right.IsConstant
                        },

                    (SQLValueType.String, SQLValueType.String)
                        => new ExpressionMetadata
                        {
                            Type = SQLValueType.String,
                            IsNullable = left.IsNullable || right.IsNullable,
                            IsConstant = left.IsConstant && right.IsConstant
                        },

                    _ => throw new SyntaxErrorException($"Operator '{op}' cannot be applied to {left.Type} and {right.Type}.")
                };
            }

            // -, *, /, %
            return (left.Type, right.Type) switch
            {
                (SQLValueType.Number, SQLValueType.Number)
                    => new ExpressionMetadata
                    {
                        Type = SQLValueType.Number,
                        IsNullable = left.IsNullable || right.IsNullable,
                        IsConstant = left.IsConstant && right.IsConstant,
                    },

                _ => throw new SyntaxErrorException($"Operator '{op}' cannot be applied to {left.Type} and {right.Type}.")
            };
        }
        private ExpressionMetadata AnalyzeLiteral(LiteralExpression literal)
        {
            if (literal.Type == SQLLiteralType.UnicodeString)
            {
                return new ExpressionMetadata
                {
                    Type = SQLValueType.String,
                    IsConstant = true,
                    IsNullable = true,
                };
            }
            else if (literal.Type == SQLLiteralType.String)
            {
                return new ExpressionMetadata
                {
                    Type = SQLValueType.String,
                    IsConstant = true,
                    IsNullable = true,
                };
            }
            else if (literal.Type == SQLLiteralType.Integer)
            {
                return new ExpressionMetadata
                {
                    Type = SQLValueType.Number,
                    IsConstant = true,
                    IsNullable = false,
                };
            }
            else if (literal.Type == SQLLiteralType.Scientific)
            {
                return new ExpressionMetadata
                {
                    Type = SQLValueType.Number,
                    IsConstant = true,
                    IsNullable = true,
                };
            }
            else if (literal.Type == SQLLiteralType.Decimal)
            {
                return new ExpressionMetadata
                {
                    Type = SQLValueType.Number,
                    IsConstant = true,
                    IsNullable = true,
                };
            }
            else if (literal.Type == SQLLiteralType.BinaryLiteral)
            {
                return new ExpressionMetadata
                {
                    Type = SQLValueType.Binary,
                    IsConstant = true,
                    IsNullable = true,
                };
            }
            else if (literal.Type == SQLLiteralType.Money)
            {
                return new ExpressionMetadata
                {
                    Type = SQLValueType.Number,
                    IsConstant = true,
                    IsNullable = true,
                };
            }
            else if (literal.Type == SQLLiteralType.Null)
            {
                return new ExpressionMetadata
                {
                    Type = SQLValueType.NullLiteral,
                    IsConstant = true,
                    IsNullable = true,
                };
            }

            throw new SyntaxErrorException($"Unexpected literal Type '{literal.Value}' ({literal.Type})");
        }
        private ExpressionMetadata AnalyzeWildcard(WildcardExpression wildcard)
        {
            if (wildcard.TableAlias != null)
            {
                var type = ResolveAlias(wildcard.TableAlias);

                return new ExpressionMetadata
                {
                    Type = SQLValueType.Unknown,
                    IsWildcard = true,
                };
            }

            return new ExpressionMetadata
            {
                Type = SQLValueType.Unknown,
                IsWildcard = true,
            };
        }
        private ExpressionMetadata AnalyzeColumn(ColumnExpression column)
        {
            if (column.Table?.Count > 1)
                throw new SyntaxErrorException($"Table Alias with the name '{string.Join(".", column.Table)}' cannot be found!");

            var type = ResolveAlias(column.Table[0]);
            var prop = type.GetProperty(column.Column);

            if (prop == null)
                throw new SyntaxErrorException($"column with the name '{column.Column}' cannot be found!");

            var mappedType = MapType(type);

            return new ExpressionMetadata
            {
                IsConstant = false,
                IsNullable = IsNullable(prop),
                Type = mappedType,
            };
        }

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
        private SQLValueType MapType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type == typeof(char))
                return SQLValueType.String;

            else if (type == typeof(string))
                return SQLValueType.String;

            else if (type == typeof(bool))
                return SQLValueType.Boolean;

            else if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
                return SQLValueType.DateTime;

            else if (type == typeof(Guid))
                return SQLValueType.Guid;

            else if (type == typeof(byte[]))
                return SQLValueType.Number;

            else if (type == typeof(short))
                return SQLValueType.Number;

            else if (type == typeof(int))
                return SQLValueType.Number;

            else if (type == typeof(long))
                return SQLValueType.Number;

            else if (type == typeof(float))
                return SQLValueType.Number;

            else if (type == typeof(double))
                return SQLValueType.Number;

            else if (type == typeof(decimal))
                return SQLValueType.Number;

            return SQLValueType.Unknown;
        }
        private bool IsNullable(PropertyInfo prop)
        {
            var type = prop.PropertyType;

            if (!type.IsValueType)
                return true; // reference types

            return Nullable.GetUnderlyingType(type) != null;
        }
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
        private bool IsLogicalOperator(SQLBinaryOperator op)
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
        bool AreComparable(ExpressionMetadata left, ExpressionMetadata right)
        {
            return left.Type == right.Type ||
               left.Type == SQLValueType.NullLiteral ||
               right.Type == SQLValueType.NullLiteral;
        }
        private bool IsUnknown(ExpressionMetadata m)
        {
            return m.Type == SQLValueType.Unknown ||
                   m.Type == SQLValueType.NullLiteral;
        }
        #endregion
    }
}

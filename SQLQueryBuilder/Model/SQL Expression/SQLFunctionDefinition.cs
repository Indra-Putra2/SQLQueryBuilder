using SQLQueryBuilder.SQLEnum;
using System.Data;
using System.Xml.Linq;

namespace SQLQueryBuilder.Model.SQL_Expression
{
    internal class SQLFunctionDefinition
    {
        private readonly Dictionary<string, SQLFunction> Functions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                // =========================
                // Aggregate
                // =========================

                ["COUNT"] = new SQLFunction
                {
                    ArgumentCount = 1,
                    FixedReturnType = SQLExpressionType.Number,
                    AcceptsWildcard = true,
                    IsNullAware = true,
                    NullabilityRule = SQLNullabilityRule.Never
                },

                ["SUM"] = new SQLFunction
                {
                    ArgumentCount = 1,
                    FixedReturnType = SQLExpressionType.Number,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                ["AVG"] = new SQLFunction
                {
                    ArgumentCount = 1,
                    FixedReturnType = SQLExpressionType.Number,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                ["MIN"] = new SQLFunction
                {
                    ArgumentCount = 1,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                ["MAX"] = new SQLFunction
                {
                    ArgumentCount = 1,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                // =========================
                // String
                // =========================

                ["LEN"] = new SQLFunction
                {
                    ArgumentCount = 1,
                    FixedReturnType = SQLExpressionType.Number,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                ["LOWER"] = new SQLFunction
                {
                    ArgumentCount = 1,
                    FixedReturnType = SQLExpressionType.String,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                ["UPPER"] = new SQLFunction
                {
                    ArgumentCount = 1,
                    FixedReturnType = SQLExpressionType.String,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                ["LTRIM"] = new SQLFunction
                {
                    ArgumentCount = 1,
                    FixedReturnType = SQLExpressionType.String,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                ["RTRIM"] = new SQLFunction
                {
                    ArgumentCount = 1,
                    FixedReturnType = SQLExpressionType.String,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                ["REVERSE"] = new SQLFunction
                {
                    ArgumentCount = 1,
                    FixedReturnType = SQLExpressionType.String,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                ["LEFT"] = new SQLFunction
                {
                    ArgumentCount = 2,
                    FixedReturnType = SQLExpressionType.String,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                ["RIGHT"] = new SQLFunction
                {
                    ArgumentCount = 2,
                    FixedReturnType = SQLExpressionType.String,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                ["SUBSTRING"] = new SQLFunction
                {
                    ArgumentCount = 3,
                    FixedReturnType = SQLExpressionType.String,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                ["REPLACE"] = new SQLFunction
                {
                    ArgumentCount = 3,
                    FixedReturnType = SQLExpressionType.String,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                ["CONCAT"] = new SQLFunction
                {
                    ArgumentCount = 2,
                    FixedReturnType = SQLExpressionType.String,
                    IsNullAware = true,
                    NullabilityRule = SQLNullabilityRule.Never
                },

                // =========================
                // Numeric
                // =========================

                ["ABS"] = new SQLFunction
                {
                    ArgumentCount = 1,
                    FixedReturnType = SQLExpressionType.Number,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                ["ROUND"] = new SQLFunction
                {
                    ArgumentCount = 2,
                    FixedReturnType = SQLExpressionType.Number,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                ["CEILING"] = new SQLFunction
                {
                    ArgumentCount = 1,
                    FixedReturnType = SQLExpressionType.Number,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                ["FLOOR"] = new SQLFunction
                {
                    ArgumentCount = 1,
                    FixedReturnType = SQLExpressionType.Number,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                ["POWER"] = new SQLFunction
                {
                    ArgumentCount = 2,
                    FixedReturnType = SQLExpressionType.Number,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                ["SQRT"] = new SQLFunction
                {
                    ArgumentCount = 1,
                    FixedReturnType = SQLExpressionType.Number,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                // =========================
                // Date / Time
                // =========================

                ["GETDATE"] = new SQLFunction
                {
                    ArgumentCount = 0,
                    FixedReturnType = SQLExpressionType.DateTime,
                    NullabilityRule = SQLNullabilityRule.Never
                },

                ["GETUTCDATE"] = new SQLFunction
                {
                    ArgumentCount = 0,
                    FixedReturnType = SQLExpressionType.DateTime,
                    NullabilityRule = SQLNullabilityRule.Never
                },

                ["SYSDATETIME"] = new SQLFunction
                {
                    ArgumentCount = 0,
                    FixedReturnType = SQLExpressionType.DateTime,
                    NullabilityRule = SQLNullabilityRule.Never
                },

                ["DATEADD"] = new SQLFunction
                {
                    ArgumentCount = 3,
                    FixedReturnType = SQLExpressionType.DateTime,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                ["DATEDIFF"] = new SQLFunction
                {
                    ArgumentCount = 3,
                    FixedReturnType = SQLExpressionType.Number,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                ["YEAR"] = new SQLFunction
                {
                    ArgumentCount = 1,
                    FixedReturnType = SQLExpressionType.Number,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                ["MONTH"] = new SQLFunction
                {
                    ArgumentCount = 1,
                    FixedReturnType = SQLExpressionType.Number,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                ["DAY"] = new SQLFunction
                {
                    ArgumentCount = 1,
                    FixedReturnType = SQLExpressionType.Number,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                // =========================
                // Conversion
                // =========================

                ["CAST"] = new SQLFunction
                {
                    ArgumentCount = 2,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                ["CONVERT"] = new SQLFunction
                {
                    ArgumentCount = 2,
                    NullabilityRule = SQLNullabilityRule.Propagate
                },

                // =========================
                // Null Handling
                // =========================

                ["ISNULL"] = new SQLFunction
                {
                    ArgumentCount = 2,
                    IsNullAware = true,
                    NullabilityRule = SQLNullabilityRule.AllNullable
                },

                ["COALESCE"] = new SQLFunction
                {
                    ArgumentCount = 2,
                    IsNullAware = true,
                    NullabilityRule = SQLNullabilityRule.AllNullable
                },

                ["NULLIF"] = new SQLFunction
                {
                    ArgumentCount = 2,
                    IsNullAware = true,
                    NullabilityRule = SQLNullabilityRule.Propagate
                }
            };

        public (SQLExpressionType type, SQLNullabilityRule? rule) GetFunctionExpressionType(FunctionExpression function, SQLValidationMode mode)
        {
            var name = function.Name;
            Functions.TryGetValue(name, out var func);

            // 1. Identify if any argument evaluates straight to a NULL literal
            bool hasNullArgument = function.Arguments.Any(x => x is LiteralExpression literal && literal.Type == SQLLiteralType.Null);
            // 2. Identify if the user explicitly typed the function as NULL (e.g., MyFunc() TYPE NULL)
            bool isExplicitlyTypedNull = function.FunctionType == SQLFunctionType.Null;
            // 3. Check if the function is a standard scalar function that breaks on NULL
            bool isStandardStrictFunction = func != null && !func.IsNullAware;

            if (isExplicitlyTypedNull || (hasNullArgument && isStandardStrictFunction))
            {
                return (SQLExpressionType.NullLiteral, null);
            }

            var args = function.Arguments.Count;
            var functionReturnType = FunctionTypeToExpressionType(function.FunctionType);

            if (func != null)
            {
                if (args != func.ArgumentCount)
                    throw new SyntaxErrorException(
                        $"Function '{name}' only accepts {func.ArgumentCount} argument(s).");

                if (!func.AcceptsWildcard && function.Arguments.Any(x => x is WildcardExpression))
                    throw new SyntaxErrorException($"Function '{function.Name}' does not accept wildcard arguments.");

                if (functionReturnType != SQLExpressionType.Unknown &&
                    func.FixedReturnType != null &&
                    functionReturnType != func.FixedReturnType)
                {
                    throw new SyntaxErrorException($"Function '{name}' always returns {func.FixedReturnType} and cannot be declared as {functionReturnType}.");
                }

                var returnType = func.FixedReturnType ?? functionReturnType;

                if (mode == SQLValidationMode.Strict && returnType == SQLExpressionType.Unknown)
                {
                    throw new SyntaxErrorException(
                        "Cannot determine function return type! " +
                        "Declare function return type like MyFunction() TYPE Number");
                }

                return (returnType, func.NullabilityRule);
            }
            else
            {
                var returnType = functionReturnType;

                if (mode == SQLValidationMode.Strict && returnType == SQLExpressionType.Unknown)
                {
                    throw new SyntaxErrorException(
                        "Cannot determine function return type! " +
                        "Declare function return type like MyFunction() TYPE Number");
                }

                return (returnType, SQLNullabilityRule.Propagate);
            }
        }

        private SQLExpressionType FunctionTypeToExpressionType(SQLFunctionType functionType)
        {
            return functionType switch
            {
                SQLFunctionType.Null => SQLExpressionType.NullLiteral,
                SQLFunctionType.Number => SQLExpressionType.Number,
                SQLFunctionType.String => SQLExpressionType.String,
                SQLFunctionType.DateTime => SQLExpressionType.DateTime,
                SQLFunctionType.Boolean => SQLExpressionType.Boolean,
                SQLFunctionType.Binary => SQLExpressionType.Binary,
                SQLFunctionType.Guid => SQLExpressionType.Guid,
                SQLFunctionType.Unknown => SQLExpressionType.Unknown,

                _ => SQLExpressionType.Unknown
            };
        }
    }
}

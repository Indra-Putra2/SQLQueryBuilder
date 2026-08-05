using SQLQueryBuilder.Model.SQL_Expression;
using SQLQueryBuilder.SQLEnum;
using System.Data;

namespace SQLQueryBuilder.Model
{
    internal class SQLFunctionDefinition
    {
        public Func<List<ExpressionMetadata>, ExpressionMetadata> Analyzer { get; init; }

        public readonly Dictionary<string, SQLFunctionDefinition> Functions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["COUNT"] = new()
            {
                Analyzer = args =>
                {
                    if (args.Count != 1)
                        throw new SyntaxErrorException("COUNT requires one argument.");

                    return new ExpressionMetadata
                    {
                        Type = SQLValueType.Number,
                        IsNullable = false,
                        IsConstant = false
                    };
                }
            },

            ["SUM"] = new()
            {
                Analyzer = args =>
                {
                    if (args.Count != 1)
                        throw new SyntaxErrorException("SUM requires one argument.");

                    return new ExpressionMetadata
                    {
                        Type = args[0].Type,
                        IsNullable = args[0].IsNullable,
                        IsConstant = args[0].IsConstant
                    };
                }
            },

            ["AVG"] = new()
            {
                Analyzer = args =>
                {
                    if (args.Count != 1)
                        throw new SyntaxErrorException("AVG requires one argument.");

                    return new ExpressionMetadata
                    {
                        Type = SQLValueType.Number,
                        IsNullable = args[0].IsNullable,
                        IsConstant = args[0].IsConstant
                    };
                }
            },

            ["MIN"] = new()
            {
                Analyzer = args =>
                {
                    if (args.Count != 1)
                        throw new SyntaxErrorException("MIN requires one argument.");

                    return new ExpressionMetadata
                    {
                        Type = args[0].Type,
                        IsNullable = args[0].IsNullable,
                        IsConstant = args[0].IsConstant
                    };
                }
            },

            ["MAX"] = new()
            {
                Analyzer = args =>
                {
                    if (args.Count != 1)
                        throw new SyntaxErrorException("MAX requires one argument.");

                    return new ExpressionMetadata
                    {
                        Type = args[0].Type,
                        IsNullable = args[0].IsNullable,
                        IsConstant = args[0].IsConstant
                    };
                }
            },

            ["LEN"] = new()
            {
                Analyzer = args =>
                {
                    if (args.Count != 1 || args[0].Type != SQLValueType.String)
                        throw new SyntaxErrorException("LEN requires a string.");

                    return new ExpressionMetadata
                    {
                        Type = SQLValueType.Number,
                        IsNullable = args[0].IsNullable,
                        IsConstant = args[0].IsConstant
                    };
                }
            },

            ["LOWER"] = new()
            {
                Analyzer = args =>
                {
                    if (args.Count != 1 || args[0].Type != SQLValueType.String)
                        throw new SyntaxErrorException("LOWER requires a string.");

                    return new ExpressionMetadata
                    {
                        Type = SQLValueType.String,
                        IsNullable = args[0].IsNullable,
                        IsConstant = args[0].IsConstant
                    };
                }
            },

            ["UPPER"] = new()
            {
                Analyzer = args =>
                {
                    if (args.Count != 1 || args[0].Type != SQLValueType.String)
                        throw new SyntaxErrorException("UPPER requires a string.");

                    return new ExpressionMetadata
                    {
                        Type = SQLValueType.String,
                        IsNullable = args[0].IsNullable,
                        IsConstant = args[0].IsConstant
                    };
                }
            },

            ["ABS"] = new()
            {
                Analyzer = args =>
                {
                    if (args.Count != 1 || args[0].Type != SQLValueType.Number)
                        throw new SyntaxErrorException("ABS requires a number.");

                    return new ExpressionMetadata
                    {
                        Type = SQLValueType.Number,
                        IsNullable = args[0].IsNullable,
                        IsConstant = args[0].IsConstant
                    };
                }
            },

            ["ROUND"] = new()
            {
                Analyzer = args =>
                {
                    if (args.Count < 1 || args[0].Type != SQLValueType.Number)
                        throw new SyntaxErrorException("ROUND requires a number.");

                    return new ExpressionMetadata
                    {
                        Type = SQLValueType.Number,
                        IsNullable = args[0].IsNullable,
                        IsConstant = args.All(x => x.IsConstant)
                    };
                }
            }
        };
    }
}

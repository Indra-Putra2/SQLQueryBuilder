using SQLQueryBuilder.Core.SQLEnum.SemanticValidator;
using SQLQueryBuilder.Core.SQLEnum.SQLExpression;
using SQLQueryBuilder.Core.SQLEnum.Tokenizer;
using System.Collections.Frozen;

namespace SQLQueryBuilder.Model
{
    internal static class SQLDictionary
    {
        // Definitions
        public static readonly string[] DateFormats =
        {
            "yyyy-MM-dd",
        };
        public static readonly string[] DateTimeFormats =
        {
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm:ss.FFFFFFF"
        };
        public static readonly string[] TimeFormats =
        {
            "HH:mm:ss",
            "HH:mm:ss.FFFFFFF"
        };

        public static readonly FrozenSet<string> SQLKeywords = new[]
        {
            "SELECT", "FROM", "WHERE", "INSERT", "UPDATE", "DELETE",
            "INTO", "VALUES", "SET",
            "JOIN", "INNER", "LEFT", "RIGHT", "FULL", "OUTER", "ON",
            "AND", "OR", "NOT", "IN", "EXISTS", "BETWEEN", "LIKE", "IS",
            "ORDER", "BY", "GROUP", "HAVING", "DISTINCT",
            "AS",
            "CASE", "WHEN", "THEN", "ELSE", "END",
            "CREATE", "ALTER", "DROP", "TABLE", "VIEW", "INDEX",
            "LIMIT", "OFFSET", "DEFAULT",
            "ASC", "DESC"
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        public static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> SQLKeywordsLookup = SQLKeywords.GetAlternateLookup<ReadOnlySpan<char>>();

        public static readonly FrozenSet<string> SQLCustomKeywords = new[]
        {
            "TYPE", "?", "CONST"
         }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        public static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> SQLCustomKeywordsLookup = SQLCustomKeywords.GetAlternateLookup<ReadOnlySpan<char>>();

        public static readonly FrozenSet<string> SQLCustomDataTypes = new[]
        {
            "INT", "DECIMAL", "DOUBLE", "STRING", "BOOL",
            "DATE", "TIME", "DATETIME", "BINARY", "GUID"
        }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        public static readonly FrozenSet<string>.AlternateLookup<ReadOnlySpan<char>> SQLCustomDataTypeLookup =
            SQLCustomDataTypes.GetAlternateLookup<ReadOnlySpan<char>>();

        public static ReadOnlySpan<int> DaysInMonth => [31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

        public static readonly Dictionary<SQLTokenType, SQLBinaryOperator> ComparisonOperators =
        new()
        {
            [SQLTokenType.Equal] = SQLBinaryOperator.Equal,
            [SQLTokenType.NotEqual] = SQLBinaryOperator.NotEqual,
            [SQLTokenType.LessThan] = SQLBinaryOperator.LessThan,
            [SQLTokenType.LessThanOrEqual] = SQLBinaryOperator.LessThanOrEqual,
            [SQLTokenType.GreaterThan] = SQLBinaryOperator.GreaterThan,
            [SQLTokenType.GreaterThanOrEqual] = SQLBinaryOperator.GreaterThanOrEqual,
        };

        public static readonly FrozenSet<SQLTokenType> DelimitedIdentifiers =
            new[]
            {
                SQLTokenType.BracketIdentifier,
                SQLTokenType.DoubleQuoteIdentifier
            }.ToFrozenSet();

        public static readonly FrozenSet<SQLTokenType> Identifiers =
            new[]
            {
                SQLTokenType.Identifier,
                SQLTokenType.BracketIdentifier,
                SQLTokenType.DoubleQuoteIdentifier
            }.ToFrozenSet();

        public static readonly FrozenSet<SQLTokenType> Literals =
            new[]
            {
                SQLTokenType.IntegerLiteral,
                SQLTokenType.DecimalLiteral,
                SQLTokenType.ScientificLiteral,
                SQLTokenType.StringLiteral,
                SQLTokenType.UnicodeString,
                SQLTokenType.DateLiteral,
                SQLTokenType.TimeLiteral,
                SQLTokenType.DatetimeLiteral,
                SQLTokenType.HexLiteral,
                SQLTokenType.MoneyLiteral,
                SQLTokenType.NullLiteral
            }.ToFrozenSet();

        public static readonly FrozenSet<SQLTokenType> Comparison =
            new[]
            {
                SQLTokenType.Equal,
                SQLTokenType.NotEqual,
                SQLTokenType.LessThan,
                SQLTokenType.LessThanOrEqual,
                SQLTokenType.GreaterThan,
                SQLTokenType.GreaterThanOrEqual
            }.ToFrozenSet();

        public static readonly FrozenSet<SQLTokenType> DelimitersAndTerminators =
            new[]
            {
                SQLTokenType.Comma,
                SQLTokenType.Semicolon,
                SQLTokenType.EOF
            }.ToFrozenSet();

        public static readonly FrozenSet<SQLTokenType> AllowedAliases =
            new[]
            {
                SQLTokenType.Identifier,
                SQLTokenType.BracketIdentifier,
                SQLTokenType.DoubleQuoteIdentifier,
            }.ToFrozenSet();

        public static readonly FrozenSet<SQLExpressionType> NumericTypes =
        new[]
        {
            SQLExpressionType.Integer,
            SQLExpressionType.Decimal,
            SQLExpressionType.Double
        }.ToFrozenSet();

        //Methods
        public static bool TryGetSQLType(ReadOnlySpan<char> typeSpan, out SQLFunctionType type)
        {
            if (typeSpan.Equals("INT", StringComparison.OrdinalIgnoreCase))
            { type = SQLFunctionType.Integer; return true; }
            if (typeSpan.Equals("DOUBLE", StringComparison.OrdinalIgnoreCase))
            { type = SQLFunctionType.Double; return true; }
            if (typeSpan.Equals("DECIMAL", StringComparison.OrdinalIgnoreCase))
            { type = SQLFunctionType.Decimal; return true; }
            if (typeSpan.Equals("STRING", StringComparison.OrdinalIgnoreCase))
            { type = SQLFunctionType.String; return true; }
            if (typeSpan.Equals("BOOL", StringComparison.OrdinalIgnoreCase))
            { type = SQLFunctionType.Boolean; return true; }
            if (typeSpan.Equals("GUID", StringComparison.OrdinalIgnoreCase))
            { type = SQLFunctionType.Guid; return true; }
            if (typeSpan.Equals("DATE", StringComparison.OrdinalIgnoreCase))
            { type = SQLFunctionType.Date; return true; }
            if (typeSpan.Equals("TIME", StringComparison.OrdinalIgnoreCase))
            { type = SQLFunctionType.Time; return true; }
            if (typeSpan.Equals("DATETIME", StringComparison.OrdinalIgnoreCase))
            { type = SQLFunctionType.DateTime; return true; }
            if (typeSpan.Equals("BINARY", StringComparison.OrdinalIgnoreCase))
            { type = SQLFunctionType.Binary; return true; }
            if (typeSpan.Equals("NULL", StringComparison.OrdinalIgnoreCase))
            { type = SQLFunctionType.Null; return true; }

            type = SQLFunctionType.Unknown;
            return false;
        }
        public static SQLExpressionType GetExpressionType(SQLFunctionType functionType)
        {
            return functionType switch
            {
                SQLFunctionType.Null => SQLExpressionType.NullLiteral,
                SQLFunctionType.Integer => SQLExpressionType.Integer,
                SQLFunctionType.Decimal => SQLExpressionType.Decimal,
                SQLFunctionType.Double => SQLExpressionType.Double,
                SQLFunctionType.String => SQLExpressionType.String,
                SQLFunctionType.Date => SQLExpressionType.Date,
                SQLFunctionType.Time => SQLExpressionType.Time,
                SQLFunctionType.DateTime => SQLExpressionType.DateTime,
                SQLFunctionType.Boolean => SQLExpressionType.Boolean,
                SQLFunctionType.Binary => SQLExpressionType.Binary,
                SQLFunctionType.Guid => SQLExpressionType.Guid,
                SQLFunctionType.Unknown => SQLExpressionType.Unknown,

                _ => SQLExpressionType.Unknown
            };
        }
        public static SQLExpressionType GetExpressionType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            return type switch
            {
                _ when type == typeof(char) || type == typeof(string) => SQLExpressionType.String,
                _ when type == typeof(bool) => SQLExpressionType.Boolean,
                _ when type == typeof(DateOnly) => SQLExpressionType.Date,
                _ when type == typeof(TimeOnly) => SQLExpressionType.Time,
                _ when type == typeof(DateTime) || type == typeof(DateTimeOffset) => SQLExpressionType.DateTime,
                _ when type == typeof(Guid) => SQLExpressionType.Guid,
                _ when type == typeof(byte[]) => SQLExpressionType.Binary,
                _ when type == typeof(int) => SQLExpressionType.Integer,
                _ when type == typeof(double) => SQLExpressionType.Double,
                _ when type == typeof(decimal) => SQLExpressionType.Decimal,

                _ => SQLExpressionType.Unknown
            };
        }
        public static Type? GetCLRType(SQLExpressionType expressionType)
        {
            return expressionType switch
            {
                SQLExpressionType.Binary => typeof(byte[]),
                SQLExpressionType.Boolean => typeof(bool),
                SQLExpressionType.Date => typeof(DateOnly),
                SQLExpressionType.Time => typeof(TimeOnly),
                SQLExpressionType.DateTime => typeof(DateTime),
                SQLExpressionType.Integer => typeof(int),
                SQLExpressionType.Decimal => typeof(decimal),
                SQLExpressionType.Double => typeof(double),
                SQLExpressionType.String => typeof(string),
                SQLExpressionType.Guid => typeof(Guid),
                SQLExpressionType.NullLiteral => null,
                _ => throw new InvalidCastException($"Expression type '{expressionType}' is not a valid CLR Type.")
            };
        }
    }
}

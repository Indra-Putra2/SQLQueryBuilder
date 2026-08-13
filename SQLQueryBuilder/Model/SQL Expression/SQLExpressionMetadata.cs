using SQLQueryBuilder.SQLEnum;

namespace SQLQueryBuilder.Model.SQL_Expression
{
    public sealed record SQLExpressionMetadata
    {
        public bool IsWildcard { get; init; } = false;
        public SQLExpressionType Type { get; init; }
        public bool IsNullable { get; init; }
        public bool IsConstant { get; init; }
    }
}

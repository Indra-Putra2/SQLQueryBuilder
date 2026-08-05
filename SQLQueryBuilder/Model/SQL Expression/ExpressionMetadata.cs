using SQLQueryBuilder.SQLEnum;

namespace SQLQueryBuilder.Model.SQL_Expression
{
    public sealed class ExpressionMetadata
    {
        public bool IsWildcard { get; init; } = false;
        public SQLValueType Type { get; init; }
        public bool IsNullable { get; init; }
        public bool IsConstant { get; init; }
    }
}

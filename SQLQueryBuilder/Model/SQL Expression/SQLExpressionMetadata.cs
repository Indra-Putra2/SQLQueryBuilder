using SQLQueryBuilder.SQLEnum.SemanticValidator;

namespace SQLQueryBuilder.Model.SQL_Expression
{
    public sealed record SQLExpressionMetadata
    {
        public Type? ClrType { get; init; }
        public SQLExpressionType Type { get; init; }
        public bool IsNullable { get; init; }
        public bool IsConstant { get; init; }
        public bool IsWildcard { get; init; } = false;
    }
}

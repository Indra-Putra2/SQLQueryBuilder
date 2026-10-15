using TruSql.Core.SQLEnum.SemanticValidator;

namespace TruSql.Model.SQL_Expression
{
    internal sealed record SQLExpressionMetadata
    {
        public Type? ClrType { get; init; }
        public SQLExpressionType Type { get; init; }
        public bool IsNullable { get; init; }
        public bool IsConstant { get; init; }
        public bool IsWildcard { get; init; } = false;
    }
}

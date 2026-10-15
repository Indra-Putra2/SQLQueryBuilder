using TruSql.Core.SQLEnum.SemanticValidator;

namespace TruSql.Model.SemanticValidator
{
    internal readonly struct SQLColumnMetadata
    {
        public required bool IsNullable { get; init; }
        public required SQLExpressionType ExpressionType { get; init; }
    }
}

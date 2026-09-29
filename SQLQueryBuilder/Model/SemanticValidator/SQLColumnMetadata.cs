using SQLQueryBuilder.Core.SQLEnum.SemanticValidator;

namespace SQLQueryBuilder.Model.SemanticValidator
{
    internal readonly struct SQLColumnMetadata
    {
        public required bool IsNullable { get; init; }
        public required SQLExpressionType ExpressionType { get; init; }
    }
}

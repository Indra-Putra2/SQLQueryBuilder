using SQLQueryBuilder.SQLEnum;

namespace SQLQueryBuilder.Model
{
    internal class SQLColumnMetadata
    {
        public required bool IsNullable { get; init; }
        public required SQLExpressionType ExpressionType { get; init; }
    }
}

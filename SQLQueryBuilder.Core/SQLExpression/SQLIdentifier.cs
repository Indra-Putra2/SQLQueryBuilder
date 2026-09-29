using SQLQueryBuilder.Core.SQLEnum.SQLExpression;

namespace SQLQueryBuilder.Core.SQLExpression
{
    public sealed class SQLIdentifier
    {
        public required string Value { get; init; } = string.Empty;
        public required SQLIdentifierDelimiter Delimiter { get; init; }
    }
}

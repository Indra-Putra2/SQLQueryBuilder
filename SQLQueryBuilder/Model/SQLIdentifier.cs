using SQLQueryBuilder.SQLEnum;

namespace SQLQueryBuilder.Model
{
    public sealed class SQLIdentifier
    {
        public required string Value { get; init; } = string.Empty;
        public required SQLIdentifierDelimiter Delimiter { get; init; }
    }
}

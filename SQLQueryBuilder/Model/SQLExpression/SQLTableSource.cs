using SQLQueryBuilder.Core.SQLExpression;
using SQLQueryBuilder.SQLEnum.QueryBuilder;

internal abstract class SQLTableSource
{
}

internal sealed class TableSource : SQLTableSource
{
    public required Type Type { get; init; }
    public required List<SQLIdentifier> TableName { get; init; } = new();
    public required SQLIdentifier Alias { get; init; }
}

internal sealed class JoinSource : SQLTableSource
{
    public required TableSource Table { get; init; }
    public required SQLExpression Condition { get; init; }
    public required SQLJoinType JoinType { get; init; }
}
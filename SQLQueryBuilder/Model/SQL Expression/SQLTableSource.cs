using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.SQL_Expression;
using SQLQueryBuilder.SQLEnum.QueryBuilder;

public abstract class SQLTableSource
{
}

public sealed class TableSource : SQLTableSource
{
    public required Type Type { get; init; }
    public required List<SQLIdentifier> TableName { get; init; } = new();
    public required SQLIdentifier Alias { get; init; }
}

public sealed class JoinSource : SQLTableSource
{
    public required TableSource Table { get; init; }
    public required SQLExpression Condition { get; init; }
    public required SQLJoinType JoinType { get; init; }
}
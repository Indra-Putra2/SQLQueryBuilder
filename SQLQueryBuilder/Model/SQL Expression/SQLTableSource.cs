using SQLQueryBuilder.Model.SQL_Expression;
using SQLQueryBuilder.SQLEnum;

public abstract class SQLTableSource
{
}

public sealed class TableSource : SQLTableSource
{
    public Type Type { get; init; }
    public List<string> TableName { get; init; } = new();
    public string Alias { get; init; }
}

public sealed class JoinSource : SQLTableSource
{
    public required TableSource Table { get; init; }
    public required SQLExpression Condition { get; init; }
    public required SQLJoinType JoinType { get; init; }
}
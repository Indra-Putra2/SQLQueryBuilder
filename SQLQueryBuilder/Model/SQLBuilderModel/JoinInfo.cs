using SQLQueryBuilder.Model.Enum;
using SQLQueryBuilder.Model.SQLBuilderModel;
using System.Collections;

public class JoinInfo
{
    public required TableRef From { get; init; }
    public required TableRef To { get; init; }
    public required JoinCondition On { get; init; }
    public required JoinType JoinType { get; init; }
    public HashSet<string> SelectedColumns { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> ColumnsMaps { get; } = new();
    public void AddMaps(IEnumerable<string> maps)
    {
        foreach (var map in maps)
        {
            if (ColumnsMaps.Contains(map)) return; 
            ColumnsMaps.Add(map);
        }
    }
    public List<string> GetMappedColumns()
    {
        return SelectedColumns
            .Zip(ColumnsMaps, (col, map) => $"{col} AS {map}")
            .Concat(SelectedColumns.Skip(ColumnsMaps.Count))
            .ToList();
    }
}
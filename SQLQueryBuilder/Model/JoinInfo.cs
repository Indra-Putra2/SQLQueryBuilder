using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.Enum;

public class JoinInfo
{
    public required JoinOnInfo LeftColumn { get; set; }
    public required JoinOnInfo RightColumn { get; set; }
    public JoinType JoinType { get; set; }

    public HashSet<string> SelectedColumns { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if this JoinInfo matches the tables and columns being requested.
    /// </summary>
    public bool IsMatch(JoinOnInfo left, JoinOnInfo right, JoinType type, string currentAlias)
    {
        return  string.Equals(LeftColumn.TableName, left.TableName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(RightColumn.TableName, right.TableName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(LeftColumn.ColumnName, left.ColumnName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(RightColumn.ColumnName, right.ColumnName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(LeftColumn.Alias, left.Alias, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(RightColumn.Alias, right.Alias, StringComparison.OrdinalIgnoreCase) &&
               JoinType == type;
    }

    public void UnionColumns(IEnumerable<string> newColumns)
    {
        foreach (var col in newColumns)
        {
            SelectedColumns.Add(col);
        }
    }
}
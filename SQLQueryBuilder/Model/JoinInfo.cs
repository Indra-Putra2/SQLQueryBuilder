using SQLQueryBuilder.Model;
using SQLQueryBuilder.Model.Enum;

public class JoinInfo
{
    public required string TableName { get; set; }
    public string Alias { get; set; }
    // The specific join condition (e.g., TableA.Id == TableB.FK)
    public required string LeftColumn { get; set; }
    public required string RightColumn { get; set; }
    public JoinType JoinType { get; set; }

    public HashSet<string> SelectedColumns { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if this JoinInfo matches the tables and columns being requested.
    /// </summary>
    public bool IsMatch(string leftName, string rightName, JoinType type, string currentAlias)
    {
        return string.Equals(LeftColumn, leftName, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(RightColumn, rightName, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(Alias, currentAlias, StringComparison.OrdinalIgnoreCase) && // Check alias!
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
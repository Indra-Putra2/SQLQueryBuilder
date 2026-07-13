using System.Text;

namespace SQLQueryBuilder.Model
{
    internal class ColumnMetadataModel
    {
        public string? OriginalQuery { get; set; }
        public string? TableAlias { get; set; }   // e.g., "r" from "r.name"
        public required string ColumnName { get; set; }   // e.g., "name" from "r.name"
        public string? Alias { get; set; }        // The final identifier name
        public required bool IsExpression { get; set; }   // True if it's a function, CASE, math, etc.
    }
}

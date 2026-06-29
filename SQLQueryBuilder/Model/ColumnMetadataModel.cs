namespace SQLQueryBuilder.Model
{
    internal class ColumnMetadataModel
    {
        public required string OriginalQuery { get; init; }
        public string? TableAlias { get; init; }   // e.g., "r" from "r.name"
        public required string ColumnName { get; init; }   // e.g., "name" from "r.name"
        public string? Alias { get; init; }        // The final identifier name
        public required bool IsExpression { get; init; }   // True if it's a function, CASE, math, etc.
    }
}

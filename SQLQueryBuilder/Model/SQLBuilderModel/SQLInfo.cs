using Dapper;

namespace SQLQueryBuilder.Model.SQLBuilderModel
{
    internal class SQLInfo
    {
        public required TableRef Root { get; init; }

        public List<JoinInfo> Joins { get; } = new();

        public HashSet<string> SelectedColumns { get; } = new(StringComparer.OrdinalIgnoreCase);
        public string? Where { get; set; }
        public DynamicParameters? Parameters { get; set; }
    }
}

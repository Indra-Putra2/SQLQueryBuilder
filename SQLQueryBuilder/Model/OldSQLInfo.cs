using Dapper;

namespace SQLQueryBuilder.Model
{
    public class OldSQLInfo
    {
        public required Type Type { get; set; }
        public required string TableName { get; set; }
        public string? Alias { get; set; }
        public required List<string> Columns { get; set; }
        public string? Where { get; set; }
        public DynamicParameters? DynamicParameters { get; set; }
        public List<OldJoinInfo>? Joins { get; set; }
    }
}

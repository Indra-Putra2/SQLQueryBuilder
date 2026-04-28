namespace SQLQueryBuilder.Model
{
    public class JoinOnInfo
    {
        public Type? TableType { get; set; }
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string? Alias { get; set; }
    }
}

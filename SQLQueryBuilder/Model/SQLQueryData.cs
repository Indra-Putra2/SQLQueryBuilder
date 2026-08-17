using Dapper;

namespace SQLQueryBuilder.Model
{
    public class SQLQueryData
    {
        public string SQL { get; set; } = string.Empty;
        public DynamicParameters? Parameters { get; set; }
    }
}

using Dapper;

namespace SQLQueryBuilder.Model
{
    public class SQLQueryResult
    {
        public string SQL { get; set; } = string.Empty;
        public DynamicParameters? Parameters { get; set; }
    }
}

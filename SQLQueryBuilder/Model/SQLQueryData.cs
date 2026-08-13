using Dapper;
using SQLQueryBuilder.Model.SQL_Expression;

namespace SQLQueryBuilder.Model
{
    public class SQLQueryData
    {
        public List<string>? QueryParts { get; set; }
        public string? SQL { get; set; }
        public DynamicParameters? Parameters { get; set; }
        public required SQLExpressionMetadata ExpressionMetadata { get; set; }
    }
}

using Dapper;

namespace SQLQueryBuilder.Model.QueryBuilder
{
    public class SQLQueryResult
    {
        private int parameterIndex = 0;
        public DynamicParameters Parameters { get; private set; } = null;
        public string SQL { get; private set; } = string.Empty;
        public int SetParameterIndex(int num) => parameterIndex = num;
        public void SetParameter(DynamicParameters param) => Parameters = param;
        public void SetSQL(string sql) => SQL = sql;
    }
}

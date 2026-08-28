using Dapper;

namespace SQLQueryBuilder.Model
{
    public class SQLQueryResult
    {
        private int parameterIndex = 0;
        public DynamicParameters Parameters { get; } = new();
        public string SQL { get; private set; } = string.Empty;
        public int NextParameterIndex() => ++parameterIndex;
        public void AddParameter(string name, object value) => Parameters.Add(name, value);
        public void AddDynamicParams(DynamicParameters param) => Parameters.AddDynamicParams(param);
        public void SetSQL(string sql) => SQL = sql;
    }
}

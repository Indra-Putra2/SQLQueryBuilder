using Dapper;

namespace SQLQueryBuilder.Model
{
    internal sealed class SQLGeneratorBase
    {
        public string SQL { get; }
        public DynamicParameters Parameters { get; }
        public int ParameterIndex { get; }

        public SQLGeneratorBase(
            string sql,
            DynamicParameters parameters,
            int parameterIndex)
        {
            SQL = sql;
            Parameters = parameters;
            ParameterIndex = parameterIndex;
        }
    }
}

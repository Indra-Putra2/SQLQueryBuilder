using System.Collections.Frozen;

namespace TruSql.Core.QueryBuilder
{
    public record SQLQueryResult(
        FrozenDictionary<string, QueryParameter> Parameters,
        string ParameterizedSQL,
        string RawSQL
    );
}
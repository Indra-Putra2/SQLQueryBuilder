using System.Collections.Frozen;

namespace SQLQueryBuilder.Core.QueryBuilder
{
    public record SQLQueryResult(
        FrozenDictionary<string, QueryParameter> Parameters,
        string ParameterizedSQL,
        string RawSQL
    );
}
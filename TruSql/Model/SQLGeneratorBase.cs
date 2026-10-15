using System.Collections.Frozen;
using TruSql.Core.QueryBuilder;

namespace TruSql.Model
{
    internal sealed class SQLGeneratorBase
    {
        public string ParametizedSQL { get; }
        public string RawSQL { get; }
        public FrozenDictionary<string, QueryParameter> Parameters { get; }

        public SQLGeneratorBase(
            string parametizedSQL,
            string rawSQL,
            Dictionary<string, QueryParameter> parameters)
        {
            ParametizedSQL = parametizedSQL;
            RawSQL = rawSQL;
            Parameters = parameters.ToFrozenDictionary();
        }
    }
}

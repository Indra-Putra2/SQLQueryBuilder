using System.Data;

namespace SQLQueryBuilder.Core.QueryBuilder
{
    public readonly struct QueryParameter
    {
        public object? Value { get; init; }
        public DbType? DbType { get; init; }
        public int? Size { get; init; }

        public QueryParameter(object? value, DbType? dbType = null, int? size = null)
        {
            Value = value ?? DBNull.Value;
            DbType = dbType;
            Size = size;
        }
    }
}

using SQLQueryBuilder.Interface;

namespace SQLQueryBuilder.SQLBuilder
{
    public static class QueryBuilder
    {
        public static IQuerySelect From<T>(string alias) => new Query<T>(alias);
    }
}

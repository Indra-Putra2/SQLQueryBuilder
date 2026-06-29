using SQLQueryBuilder.Interface;

namespace SQLQueryBuilder.Builder
{
    public class QueryBuilder
    {
        public IQuerySelect From<T>(string alias) => new Query<T>(alias);
    }
}

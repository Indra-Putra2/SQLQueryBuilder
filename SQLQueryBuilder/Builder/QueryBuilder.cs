using SQLQueryBuilder.Interface;

namespace SQLQueryBuilder.Builder
{
    public static class QueryBuilder
    {
        public static IQueryFrom Create<TBase>()
        {
            return new QueryBuilderImpl<TBase>();
        }
    }
}

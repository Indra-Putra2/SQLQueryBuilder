using TruSql.Interface;

namespace TruSql.Builder
{
    public static class QueryBuilder
    {
        public static IQueryFrom Create<TBase>()
        {
            return new QueryBuilderImpl<TBase>();
        }
    }
}

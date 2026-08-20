using SQLQueryBuilder.Interface;
using SQLQueryBuilder.SQLEnum;

namespace SQLQueryBuilder.Builder
{
    public class QueryBuilder
    {
        public static IQueryFrom Create<TBase>(SQLValidationMode mode)
        {
            return new QueryBuilderImpl<TBase>(mode);
        }
    }
}

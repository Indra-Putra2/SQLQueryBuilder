using SQLQueryBuilder.Model.QueryBuilder;
using SQLQueryBuilder.SQLEnum.QueryBuilder;

namespace SQLQueryBuilder.Interface
{
    public interface IQueryFrom
    {
        IQuerySelect From<T>(string tableAlias);
    }

    public interface IQuerySelect
    {
        IQuery Select(string colums);
    }
    public interface IQuery
    {
        IQuery Join<T>(string tableAlias, string joinCondition, SQLJoinType joinType = SQLJoinType.INNER);
        IQueryFilter OrderBy(string? order);
        IQueryFilter GroupBy(string? groupBy);
        IQueryFilter Having(string? having);
        IQueryFilter Where(string? where);
        IQueryFilter ValidateBaseQuery();
        IQueryFilter Limit(int? limit);
        SQLQueryResult Build();
    }

    public interface IQueryFilter
    {
        IQueryFilter CopyBase();
        IQueryFilter OrderBy(string? order);
        IQueryFilter GroupBy(string? groupBy);
        IQueryFilter Having(string? having);
        IQueryFilter Where(string? where);
        IQueryFilter Limit(int? limit);
        SQLQueryResult Build();
    }
}

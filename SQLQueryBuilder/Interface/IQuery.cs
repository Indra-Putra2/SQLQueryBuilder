using Dapper;
using SQLQueryBuilder.Enum;
using ASTBuilder.Abstraction;

namespace SQLQueryBuilder.Interface
{
    public interface IQuerySelect
    {
        IQuery Select(string colums);
    }
    public interface IQuery
    {
        IQuery MapField(params (string columnName, string alias)[] maps);
        IQuery Join<T>(string tableAlias, string join, JoinTypeEnum joinType = JoinTypeEnum.INNER);
        IQueryFilter Where(IExpression expr = null!);
        IQueryFilter OrderBy(string orderBy = null!, OrderTypeEnum order = OrderTypeEnum.DESC);
        IQueryFilter GroupBy(string groupBy = null!);
        (string, DynamicParameters) Build();
    }

    public interface IQueryFilter
    {
        IQueryFilter OrderBy(string orderBy, OrderTypeEnum order = OrderTypeEnum.DESC);
        IQueryFilter GroupBy(string groupBy);
        (string, DynamicParameters) Build();
    }
}

using Dapper;
using SQLQueryBuilder.Model.ASTModel;
using SQLQueryBuilder.Model.Enum;

namespace SQLQueryBuilder.Model.Interface
{
    public interface IQuerySelect
    {
        IQuery Select(string colums);
    }
    public interface IQuery
    {
        IQuery MapField(params (string columnName, string alias)[] maps);
        IQuery Join<T>(string tableAlias, string join, JoinTypeEnum joinType = JoinTypeEnum.INNER);
        IQueryFilter Where(Expr expr);
        IQueryFilter OrderBy(string orderBy, OrderTypeEnum order = Model.Enum.OrderTypeEnum.DESC);
        IQueryFilter GroupBy(string groupBy);
        (string, DynamicParameters) Build();
    }

    public interface IQueryFilter
    {
        IQueryFilter OrderBy(string orderBy, OrderTypeEnum order = Model.Enum.OrderTypeEnum.DESC);
        IQueryFilter GroupBy(string groupBy);
        (string, DynamicParameters) Build();
    }


}

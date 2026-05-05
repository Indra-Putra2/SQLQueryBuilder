using Dapper;
using SQLQueryBuilder.Model.ASTModel;
using SQLQueryBuilder.Model.Enum;

namespace SQLQueryBuilder.Model.Interface
{
    public interface IQueryStart
    {
        IQuery From<T>(string tableAlias = null);
        (string, DynamicParameters) Build();
    }

    public interface IQuery
    {
        IQuery Select(string colums);
        IQuery Join<T>(string tableAlias, string join, JoinTypeEnum joinType = JoinTypeEnum.INNER);
        IQueryAfterWhere Where(Expr expr);
        IQuery OrderBy(string orderBy, OrderTypeEnum order = Model.Enum.OrderTypeEnum.DESC);
        IQuery GroupBy(string groupBy);

        (string, DynamicParameters) Build();
    }

    public interface IQueryAfterWhere
    {
        IQuery OrderBy(string orderBy, OrderTypeEnum order = Model.Enum.OrderTypeEnum.DESC);
        IQuery GroupBy(string groupBy);
        (string, DynamicParameters) Build();
    }

}

using Dapper;
using SQLQueryBuilder.Enum;
using ASTBuilder.Core.ASTModel;

namespace SQLQueryBuilder.Interface
{
    public interface IQuerySelect
    {
        IQuery Select(string colums);
    }
    public interface IQuery
    {
        IQuery Join<T>(string tableAlias, string join, JoinTypeEnum joinType = JoinTypeEnum.INNER);
        IQueryFilter Where(ExpressionModel expr = null!);
        IQueryFilter OrderBy(string orderBy = null!, OrderTypeEnum order = OrderTypeEnum.DESC);
        IQueryFilter GroupBy(string groupBy = null!);
        (string sql, DynamicParameters param) Build();
        IQueryFilter CompileSqlBase();
    }

    public interface IQueryFilter
    {
        IQueryFilter Where(ExpressionModel expr = null!);
        IQueryFilter OrderBy(string orderBy, OrderTypeEnum order = OrderTypeEnum.DESC);
        IQueryFilter GroupBy(string groupBy);
        (string sql, DynamicParameters param) Build();
    }
}

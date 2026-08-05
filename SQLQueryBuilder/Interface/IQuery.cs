using Dapper;
using SQLQueryBuilder.SQLEnum;
using ASTBuilder.Core.ASTModel;
using SQLQueryBuilder.Model;

namespace SQLQueryBuilder.Interface
{
    public interface IQueryFrom
    {
        IQuerySelect From<T>(string? tableAlias);
    }

    public interface IQuerySelect
    {
        IQuery Select(string colums);
    }
    public interface IQuery
    {
        IQuery Join<T>(string tableAlias, string joinCondition, SQLJoinType joinType = SQLJoinType.INNER);
        IQueryFilter OrderBy(string order);
        IQueryFilter GroupBy(string groupBy);
        IQueryFilter Having(string having);
        IQueryFilter Where(string where);
        IQueryFilter Validate();
        (string sql, DynamicParameters param) Build();
    }

    public interface IQueryFilter
    {
        IQueryFilter OrderBy(string order);
        IQueryFilter GroupBy(string groupBy);
        IQueryFilter Having(string having);
        IQueryFilter Where(string where);
        (string sql, DynamicParameters param) Build();
    }
}

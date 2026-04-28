using Dapper;
using SQLQueryBuilder.Model.Enum;
using System.Linq.Expressions;

namespace SQLQueryBuilder.Model.Interface
{
    public interface IQuerySelect<T>
    {
        IQueryAfterSelect<T> Select(Expression<Func<T, object>> select = null, string tableAlias = null);
    }

    public interface IQueryAfterSelect<T>
    {
        IQueryAfterSelect<T> Join<TJoin>(
            Expression<Func<T, TJoin, bool>> on,
            Expression<Func<TJoin, object>> select = null,
            JoinType joinType = JoinType.INNER,
            string tableAlias = null);
        IQueryAfterSelect<T> Where(Expr expr);
        (string sql, DynamicParameters parameters) Build();
    }
}

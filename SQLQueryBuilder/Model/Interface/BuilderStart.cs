using Dapper;
using SQLQueryBuilder.Model.ASTModel;
using SQLQueryBuilder.Model.Enum;
using System.Linq.Expressions;

namespace SQLQueryBuilder.Model.Interface
{
    public interface IQuerySelect<T>
    {
        IQueryAfterSelect<T> Select(Expression<Func<T, object>> select = null);
    }

    public interface IQueryAfterSelect<T>
    {
        IQueryAfterSelect<T> Join<TJoin>(
            Expression<Func<T, TJoin, bool>> on,
            Expression<Func<TJoin, object>> select = null,
            Expression<Func<T, object>> map = null,
            JoinType joinType = JoinType.INNER);
        IQueryAfterSelect<T> Where(Expr expr);
        (string sql, DynamicParameters parameters) Build();
    }
}

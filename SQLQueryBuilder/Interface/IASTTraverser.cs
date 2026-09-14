using SQLQueryBuilder.Model.SQL_Expression;

namespace SQLQueryBuilder.Interface
{
    public interface IASTTraverser
    {
        void Walk(SQLExpression expression);
    }
}

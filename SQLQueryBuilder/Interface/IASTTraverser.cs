using SQLQueryBuilder.Core.SQLExpression;

namespace SQLQueryBuilder.Interface
{
    internal interface IASTTraverser
    {
        void Walk(SQLExpression expression);
    }
}

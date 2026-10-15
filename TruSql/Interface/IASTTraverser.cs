using TruSql.Core.SQLExpression;

namespace TruSql.Interface
{
    internal interface IASTTraverser
    {
        void Walk(SQLExpression expression);
    }
}

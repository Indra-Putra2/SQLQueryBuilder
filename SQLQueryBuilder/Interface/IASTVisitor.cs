using SQLQueryBuilder.Model.SQL_Expression;

namespace SQLQueryBuilder.Interface
{
    public interface IASTVisitor
    {
        void OnBinary(BinaryExpression binary);
        void OnLiteral(LiteralExpression literal);
    }
}

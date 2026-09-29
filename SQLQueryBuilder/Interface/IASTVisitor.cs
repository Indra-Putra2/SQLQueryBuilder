using SQLQueryBuilder.Core.SQLExpression;

namespace SQLQueryBuilder.Interface
{
    internal interface IASTVisitor
    {
        void OnBinary(BinaryExpression binary);
        void OnLiteral(LiteralExpression literal);
        void OnColumn(ColumnExpression column);
        void OnUnary(UnaryExpression unary);
        void OnFucntion(FunctionExpression function);
        void OnCase(CaseExpression @case);
        void OnWildcard(WildcardExpression wildcard);
        void OnIsNull(IsNullExpression isNull);
        void OnLike(LikeExpression like);
        void OnIn(InExpression @in);
        void OnBetween(BetweenExpression between);
        void OnParenthesized(ParenthesizedExpression parenthesized);
        void OnSelect(SelectExpression select);
        void OnOrderby(OrderByExpression orderBy);
    }
}

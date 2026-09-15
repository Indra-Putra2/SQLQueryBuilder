using SQLQueryBuilder.Model.SQL_Expression;

namespace SQLQueryBuilder.Interface
{
    public interface IASTVisitor
    {
        void OnBinary(BinaryExpression binary);
        void OnLiteral(LiteralExpression literal);
        void OnColumn(ColumnExpression column);
        void OnUnary(UnaryExpression unary);
        void OnFucntion(FunctionExpression function);
        void OnCase(CaseExpression @case);
        void OncaseWhen(CaseWhen caseWhen);
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

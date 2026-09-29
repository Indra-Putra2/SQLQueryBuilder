namespace SQLQueryBuilder.Core.SQLExpression
{
    public readonly record struct SQLSourceSpan(
        int Start,
        int Length)
    {
        public int End => Start + Length;
    }
}

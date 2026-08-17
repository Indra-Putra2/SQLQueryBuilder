namespace SQLQueryBuilder.Model.SQL_Expression
{
    public readonly record struct SQLSourceSpan(
        int Start,
        int Length)
    {
        public int End => Start + Length;
    }
}

using SQLQueryBuilder.Model.SQL_Expression;

namespace SQLQueryBuilder.Model
{
    public sealed record SQLQuery
    {
        public Type? OutputDTO { get; set; } = null;

        public SQLSource<TableSource>? From { get; set; } = null;
        public SQLSource<List<SelectExpression>>? Select { get; set; } = null;
        public List<SQLSource<JoinSource>> Joins { get; set; } = new();

        public SQLSource<SQLExpression>? Having { get; set; } = null;
        public SQLSource<SQLExpression>? Where { get; set; } = null;

        public SQLSource<List<SQLExpression>>? GroupBy { get; set; } = null;
        public SQLSource<List<OrderByExpression>>? OrderBy { get; set; } = null;
        public int? Limit { get; set; }

        public SQLQuery CreateBaseCopy()
        {
            return this with
            {
                Having = null,
                Where = null,
                GroupBy = null,
                OrderBy = null,
            };
        }
    }
}

using SQLQueryBuilder.Core.SQLExpression;

namespace SQLQueryBuilder.Model
{
    internal sealed record SQLQuery
    {
        public TableSource From { get; set; }
        public List<JoinSource> Joins { get; set; } = new();
        public List<SelectExpression> Select { get; set; } = new();

        public SQLExpression? Having { get; set; } = null;
        public SQLExpression? Where { get; set; } = null;

        public List<SQLExpression>? GroupBy { get; set; } = null;
        public List<OrderByExpression>? OrderBy { get; set; } = null;
        public int? Limit { get; set; } = null;
    }
}

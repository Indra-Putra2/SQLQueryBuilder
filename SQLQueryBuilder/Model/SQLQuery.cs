using SQLQueryBuilder.Model.SQL_Expression;

namespace SQLQueryBuilder.Model
{
    public sealed class SQLQuery
    {
        public TableSource From { get; set; } = new();
        public List<SelectExpression> Select { get; set; } = new();
        public List<JoinSource> Joins { get; set; } = new();
        public SQLExpression? Having { get; set; }
        public SQLExpression? Where { get; set; }
        public List<SQLExpression>? GroupBy { get; set; }
        public List<OrderByExpression>? OrderBy { get; set; }
    }
}

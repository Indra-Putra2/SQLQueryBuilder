using SQLQueryBuilder.Model.SQL_Expression;

namespace SQLQueryBuilder.Model
{
    public sealed class SQLQuery
    {
        public Type? OutputDTO { get; set; } = null;

        public Source<TableSource>? From { get; set; } = null;
        public Source<List<SelectExpression>>? Select { get; set; } = null;
        public List<Source<JoinSource>> Joins { get; set; } = new();

        public Source<SQLExpression>? Having { get; set; } = null;
        public Source<SQLExpression>? Where { get; set; } = null;

        public Source<List<SQLExpression>>? GroupBy { get; set; } = null;
        public Source<List<OrderByExpression>>? OrderBy { get; set; } = null;
    }
}

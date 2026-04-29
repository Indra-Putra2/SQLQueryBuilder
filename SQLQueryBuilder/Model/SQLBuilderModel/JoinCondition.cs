namespace SQLQueryBuilder.Model.SQLBuilderModel
{
    public class JoinCondition
    {
        public required string FromColumn { get; init; } // existing table
        public required string ToColumn { get; init; }   // new table
    }
}

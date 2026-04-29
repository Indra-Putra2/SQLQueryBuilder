namespace SQLQueryBuilder.Model.SQLBuilderModel
{
    public class TableRef
    {
        public required Type Type { get; init; }
        public required string Name { get; init; }
        public required string Alias { get; init; }
    }
}

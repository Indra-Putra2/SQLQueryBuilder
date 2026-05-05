namespace SQLQueryBuilder.Model.SQLBuilderModel
{
    public class TableRef
    {
        public required Type Type { get; init; }

        private string _name = null!;
        public required string Name
        {
            get => _name;
            init => _name = value.Trim() ?? throw new ArgumentNullException("Table Name Cannot Be Null!");
        }

        private string _alias = null!;
        public required string Alias
        {
            get => _alias;
            init => _alias = value.Trim() ?? throw new ArgumentNullException("Table Alias Cannot Be Null!");
        }
    }
}

namespace SQLQueryBuilder.Model
{
    internal class TableRefModel
    {
        public required Type Type { get; init; }

        private string _name = null!;
        public required string Name
        {
            get => _name;
            init
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Table Name cannot be null or whitespace.", nameof(Name));
                _name = value.Trim();
            }
        }

        private string _alias = null!;
        public required string Alias
        {
            get => _alias;
            init
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Table Alias cannot be null or whitespace.", nameof(Alias));
                _alias = value.Trim();
            }
        }
    }
}
namespace SQLQueryBuilder.Model.SemanticValidator
{
    internal class SQLTableMetadata
    {
        private bool _initialized = false;
        private readonly Dictionary<string, SQLColumnMetadata> _columns = new(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyDictionary<string, SQLColumnMetadata> Columns => _columns;

        public void AddColumn(string name, SQLColumnMetadata metadata)
        {
            if (_initialized)
                throw new InvalidOperationException("Table metadata is already initialized.");

            _columns.Add(name, metadata);
        }

        public void Freeze()
        {
            _initialized = true;
        }
    }
}

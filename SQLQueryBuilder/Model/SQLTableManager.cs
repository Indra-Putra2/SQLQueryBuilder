using SQLQueryBuilder.SQLEnum;
using System.Collections.ObjectModel;
using System.Data;
using System.Reflection;

namespace SQLQueryBuilder.Model
{
    internal class SQLTableManager
    {
        private NullabilityInfoContext _nullabilityContext = new NullabilityInfoContext();
        private Dictionary<string, Dictionary<string, SQLColumnMetadata>> _tables { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public void RegisterTable(string tableAlias, Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (string.IsNullOrWhiteSpace(tableAlias))
                throw new ArgumentNullException($"Table alias for type {type.FullName} cannot be empty or null! ");
            
            if (_tables.ContainsKey(tableAlias))
            {
                throw new SyntaxErrorException($"Table alias '{tableAlias}' already exists!");
            }
            
            var columns = new Dictionary<string, SQLColumnMetadata>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in type.GetProperties())
            {
                var name = prop.Name;
                var nullable = IsNullable(prop);
                var expressionType = SQLDictionary.GetExpressionType(prop.PropertyType);

                columns[name] = new SQLColumnMetadata { IsNullable = nullable, ExpressionType = expressionType};
            }

            _tables[tableAlias] = columns;
        }
        public SQLColumnMetadata GetColumnMetadata(string tableAlias, string columnName)
        {
            if (!_tables.TryGetValue(tableAlias, out var columns))
                throw new SyntaxErrorException(
                    $"Unknown table alias '{tableAlias}'.");

            if (!columns.TryGetValue(columnName, out var metadata))
                throw new SyntaxErrorException(
                    $"Column '{columnName}' does not exist on table '{tableAlias}'.");

            return metadata;
        }
        public ReadOnlyDictionary<string, SQLColumnMetadata> GetTableColumns(string tableAlias)
        {
            if (!_tables.TryGetValue(tableAlias, out var columns))
                throw new SyntaxErrorException($"Unknown table alias '{tableAlias}'.");

            return new ReadOnlyDictionary<string, SQLColumnMetadata>(columns);
        }
        public ReadOnlyDictionary<string, ReadOnlyDictionary<string, SQLColumnMetadata>> GetAllTables()
        {
            var readOnlyCopy = _tables.ToDictionary(
                kvp => kvp.Key,
                kvp => new ReadOnlyDictionary<string, SQLColumnMetadata>(kvp.Value)
            );

            return new ReadOnlyDictionary<string, ReadOnlyDictionary<string, SQLColumnMetadata>>(readOnlyCopy);
        }

        private bool IsNullable(PropertyInfo prop)
        {
            var nullability = _nullabilityContext.Create(prop);
            return nullability.ReadState == NullabilityState.Nullable;
        }
    }
}

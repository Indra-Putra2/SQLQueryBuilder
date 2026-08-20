using SQLQueryBuilder.SQLEnum;
using System.Collections.ObjectModel;
using System.Data;
using System.Reflection;

namespace SQLQueryBuilder.Model
{
    internal class SQLTableManager
    {
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
                var expressionType = MapType(prop.PropertyType);

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

        private SQLExpressionType MapType(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (type == typeof(char))
                return SQLExpressionType.String;

            else if (type == typeof(string))
                return SQLExpressionType.String;

            else if (type == typeof(bool))
                return SQLExpressionType.Boolean;

            else if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
                return SQLExpressionType.DateTime;

            else if (type == typeof(Guid))
                return SQLExpressionType.Guid;

            else if (type == typeof(byte[]))
                return SQLExpressionType.Binary;

            else if (type == typeof(short))
                return SQLExpressionType.Number;

            else if (type == typeof(int))
                return SQLExpressionType.Number;

            else if (type == typeof(long))
                return SQLExpressionType.Number;

            else if (type == typeof(float))
                return SQLExpressionType.Number;

            else if (type == typeof(double))
                return SQLExpressionType.Number;

            else if (type == typeof(decimal))
                return SQLExpressionType.Number;

            return SQLExpressionType.Unknown;
        }
        private bool IsNullable(PropertyInfo prop)
        {
            var nullabilityContext = new NullabilityInfoContext();
            var nullability = nullabilityContext.Create(prop);
            return nullability.ReadState == NullabilityState.Nullable;
        }
    }
}

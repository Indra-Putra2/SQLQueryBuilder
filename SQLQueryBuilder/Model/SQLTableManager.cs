using SQLQueryBuilder.Helper;
using SQLQueryBuilder.Model.SQL_Expression;
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

                columns[name] = new SQLColumnMetadata { IsNullable = nullable, ExpressionType = expressionType };
            }

            _tables[tableAlias] = columns;
        }
        public SQLColumnMetadata GetColumnMetadata(string originalText, ColumnExpression column)
        {
            if (column.Table == null)
            {
                var err = ErrorFormatter.Create(
                    originalText,
                    column.Span.Start,
                    $"Table Name cannot be empty.",
                    column.Span.Length);

                throw new SyntaxErrorException(err);
            }

            if (!_tables.TryGetValue(column.Table[0].Value, out var columns))
            {
                var err = ErrorFormatter.Create(
                    originalText,
                    column.Span.Start,
                    $"Unknown table alias '{column.Table[0].Value}'.",
                    column.Span.Length);

                throw new SyntaxErrorException(err);
            }


            if (!columns.TryGetValue(column.Column.Value, out var metadata))
            {
                var err = ErrorFormatter.Create(
                    originalText,
                    column.Span.Start,
                    $"Column '{column.Column.Value}' does not exist on table '{column.Table[0].Value}'.",
                    column.Span.Length);

                throw new SyntaxErrorException(err);
            }

            return metadata;
        }
        public ReadOnlyDictionary<string, SQLColumnMetadata> GetTableColumns(string originalText, WildcardExpression wildcard)
        {
            if (wildcard.TableAlias == null)
            {
                var err = ErrorFormatter.Create(
                    originalText,
                    wildcard.Span.Start,
                    $"Table Name cannot be empty.",
                    wildcard.Span.Length);

                throw new SyntaxErrorException(err);
            }

            if (!_tables.TryGetValue(wildcard.TableAlias[0].Value, out var columns))
            {
                var err = ErrorFormatter.Create(
                    originalText,
                    wildcard.Span.Start,
                    $"Unknown table alias '{wildcard.TableAlias[0].Value}'.",
                    wildcard.Span.Length);

                throw new SyntaxErrorException(err);
            }

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

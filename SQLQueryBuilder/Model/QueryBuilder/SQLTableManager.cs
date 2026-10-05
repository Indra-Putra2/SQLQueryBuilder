using SQLQueryBuilder.Core.SQLExpression;
using SQLQueryBuilder.Helper;
using SQLQueryBuilder.Model.SemanticValidator;
using System.Data;
using System.Reflection;

namespace SQLQueryBuilder.Model.QueryBuilder
{
    internal class SQLTableManager
    {
        private NullabilityInfoContext _nullabilityContext = new NullabilityInfoContext();
        private Dictionary<string, SQLTableMetadata> _tables = new(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyDictionary<string, SQLTableMetadata> Tables => _tables;

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

            var table = new SQLTableMetadata();
            foreach (var prop in type.GetProperties())
            {
                var name = prop.Name;
                var nullable = IsNullable(prop);
                var expressionType = SQLDictionary.GetExpressionType(prop.PropertyType);
                var metadata = new SQLColumnMetadata { IsNullable = nullable, ExpressionType = expressionType };
                table.AddColumn(name, metadata);
            }

            table.Freeze();
            _tables[tableAlias] = table;
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

            if (!_tables.TryGetValue(column.Table[0].Value, out var tableMetadata))
            {
                var err = ErrorFormatter.Create(
                    originalText,
                    column.Span.Start,
                    $"Unknown table alias '{column.Table[0].Value}'.",
                    column.Span.Length);

                throw new SyntaxErrorException(err);
            }


            if (!tableMetadata.Columns.TryGetValue(column.Column.Value, out var metadata))
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
        public IReadOnlyDictionary<string, SQLColumnMetadata> GetTableColumns(string originalText, WildcardExpression wildcard)
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

            if (!_tables.TryGetValue(wildcard.TableAlias[0].Value, out var tableMetadata))
            {
                var err = ErrorFormatter.Create(
                    originalText,
                    wildcard.Span.Start,
                    $"Unknown table alias '{wildcard.TableAlias[0].Value}'.",
                    wildcard.Span.Length);

                throw new SyntaxErrorException(err);
            }

            return tableMetadata.Columns;
        }
        private bool IsNullable(PropertyInfo prop)
        {
            var nullability = _nullabilityContext.Create(prop);
            return nullability.ReadState == NullabilityState.Nullable;
        }
    }
}

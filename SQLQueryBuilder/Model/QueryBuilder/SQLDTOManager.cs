using SQLQueryBuilder.Model.SQL_Expression;
using System.Data;
using System.Reflection;

namespace SQLQueryBuilder.Model.QueryBuilder
{
    internal class SQLDTOManager
    {
        private NullabilityInfoContext _nullabilityContext = new();
        private readonly Dictionary<string, PropertyInfo> _properties = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _usedOutputNames = new(StringComparer.OrdinalIgnoreCase);

        public void RegisterDTO(Type type)
        {
            foreach (var property in type.GetProperties())
            {
                _properties[property.Name] = property;
            }
        }
        // Column Output Registration
        public void RegisterOutput(string name)
        {
            if (!_properties.TryGetValue(name, out var property))
                throw new SyntaxErrorException(
                    $"DTO property '{name}' does not exist.");

            if (!_usedOutputNames.Add(name))
                throw new SyntaxErrorException(
                    $"Duplicate output column name '{name}' is not allowed.");
        }
        // Alias Output Registration
        public void RegisterOutput(string alias, SQLExpressionMetadata metadata)
        {
            if (!_properties.TryGetValue(alias, out var property))
                throw new SyntaxErrorException(
                    $"DTO property '{alias}' does not exist.");

            var sqlType = SQLDictionary.GetCLRType(metadata.Type);
            var dtoType = property.PropertyType;

            if (sqlType != null)
            {
                var dtoUnderlyingType = Nullable.GetUnderlyingType(dtoType);

                // For value types, decimal? → decimal.
                // For normal types, dtoUnderlyingType is null.
                var actualDtoType = dtoUnderlyingType ?? dtoType;

                if (actualDtoType != sqlType)
                {
                    throw new SyntaxErrorException(
                        $"Output column '{alias}' has type '{sqlType}', " +
                        $"but DTO property '{alias}' expects type '{dtoType}'.");
                }
            }

            var nullability = _nullabilityContext.Create(property);

            if (metadata.IsNullable)
            {
                // Nullable<T>
                if (dtoType.IsValueType)
                {
                    if (Nullable.GetUnderlyingType(dtoType) == null)
                    {
                        throw new SyntaxErrorException(
                            $"Output column '{alias}' may return NULL, " +
                            $"but DTO property '{alias}' is non-nullable.");
                    }
                }
                // string?, byte[]?, etc.
                else if (nullability.ReadState == NullabilityState.NotNull)
                {
                    throw new SyntaxErrorException(
                        $"Output column '{alias}' may return NULL, " +
                        $"but DTO property '{alias}' is non-nullable.");
                }
            }

            if (!_usedOutputNames.Add(alias))
                throw new SyntaxErrorException($"Duplicate output column name '{alias}' is not allowed.");
        }
    }
}

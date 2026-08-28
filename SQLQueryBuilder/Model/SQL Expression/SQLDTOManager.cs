using SQLQueryBuilder.SQLEnum;
using System.Data;
using System.Reflection;

namespace SQLQueryBuilder.Model.SQL_Expression
{
    internal class SQLDTOManager
    {
        private NullabilityInfoContext _nullabilityContext = new();
        private readonly Dictionary<string, PropertyInfo> _properties = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _usedOutputNames = new(StringComparer.OrdinalIgnoreCase);

        public void RegisterDTO(Type type)
        {
            foreach(var property in type.GetProperties())
            {
                _properties[property.Name] = property;
            }
        }

        public void RegisterOutput(string name)
        {
            if (!_properties.TryGetValue(name, out var property))
                throw new SyntaxErrorException(
                    $"DTO property '{name}' does not exist.");

            if (!_usedOutputNames.Add(name))
                throw new SyntaxErrorException(
                    $"Duplicate output column name '{name}' is not allowed.");
        }

        public void RegisterOutput(string alias, SQLExpressionMetadata metadata)
        {

            if (!_properties.TryGetValue(alias, out var property))
                throw new SyntaxErrorException(
                    $"DTO property '{alias}' does not exist.");

            var propType = property.PropertyType;
            var type = SQLDictionary.GetCLRType(metadata.Type);
            if (type != null && type != propType)
            {
                throw new SyntaxErrorException(
                    $"Output column '{alias}' has type '{type}', " +
                    $"but DTO property '{alias}' expects type '{propType}'.");
            }

            var nullability = _nullabilityContext.Create(property);
            if (metadata.IsNullable && nullability.ReadState == NullabilityState.NotNull)
            {
                throw new SyntaxErrorException(
                    $"Output column '{alias}' may return NULL, " +
                    $"but DTO property '{alias}' is non-nullable.");
            }

            if (!_usedOutputNames.Add(alias))
                throw new SyntaxErrorException(
                    $"Duplicate output column name '{alias}' is not allowed.");
        }
    }
}

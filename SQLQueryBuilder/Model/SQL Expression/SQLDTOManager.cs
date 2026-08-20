using System.Data;

namespace SQLQueryBuilder.Model.SQL_Expression
{
    internal class SQLDTOManager
    {
        private readonly HashSet<string> _properties = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _usedOutputNames = new(StringComparer.OrdinalIgnoreCase);

        public void RegisterDTO(Type type)
        {
            foreach(var property in type.GetProperties())
            {
                _properties.Add(property.Name);
            }
        }

        public void RegisterOutput(string name)
        {
            if (!_properties.Contains(name))
                throw new SyntaxErrorException(
                    $"DTO property '{name}' does not exist.");

            if (!_usedOutputNames.Add(name))
                throw new SyntaxErrorException(
                    $"Duplicate output column name '{name}' is not allowed.");
        }
    }
}

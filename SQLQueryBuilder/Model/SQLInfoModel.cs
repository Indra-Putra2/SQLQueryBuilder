using Dapper;
using SQLQueryBuilder.Attributes;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace SQLQueryBuilder.Model
{
    /// <summary>
    /// Internal entity container that maintains state and valid structural rules for a single SQL query execution cycle.
    /// Tracks mapped column references, table targets, aliases, and filters.
    /// </summary>
    internal class SQLInfoModel
    {
        // Tracks validated columns present on the entity schemas to catch syntax mistakes early
        private HashSet<string> _columnNames = new(StringComparer.OrdinalIgnoreCase);

        // Tracks manual mappings between custom field tracking keys and their true column counterparts
        private Dictionary<string, string> _columnMaps = new(StringComparer.OrdinalIgnoreCase);

        private TableRefModel _root = null!;

        public required TableRefModel Root
        {
            get => _root;
            init
            {
                _root = value;
                _columnNames = GetColumnName(); // Scans target model fields as soon as the baseline root table is initialized
            }
        }

        private string _selectedColumns = null!;
        public string SelectedColumns
        {
            get => _selectedColumns;
            set => _selectedColumns =
                string.IsNullOrWhiteSpace(value)
                    ? throw new ArgumentException("You must specify columns.", nameof(SelectedColumns))
                    : value.Trim();
        }

        public List<JoinInfoModel> Joins { get; set; } = new();

        private string _where = string.Empty;
        public string Where
        {
            get => _where;
            set => _where = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private string _orderBy = string.Empty;
        public string OrderBy
        {
            get => _orderBy;
            set => _orderBy = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private string _groupBy = string.Empty;
        public string GroupBy
        {
            get => _groupBy;
            set => _groupBy = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        /// <summary> Parameter state definitions to pass down cleanly into consumer layers like Dapper. </summary>
        public DynamicParameters? Parameters { get; set; }

        /// <summary>
        /// Validates a raw column string input name against known properties. 
        /// Returns the fully qualified statement snippet format (e.g., "alias.ColumnName").
        /// </summary>
        public string GetMappedColumnName(string columnName)
        {
            // Fails early if application layer code tries filtering against an invalid or completely missing entity field context
            if (!_columnNames.TryGetValue(columnName, out var rawName))
            {
                throw new InvalidFilterCriteriaException($"Cannot Find Column With The Name '{columnName}'");
            }

            // Checks if a custom manual field map override registration exists for this property
            if (_columnMaps.TryGetValue(rawName, out var colWithAlias))
            {
                return colWithAlias;
            }

            return $"{Root.Alias}.{rawName}";
        }

        /// <summary> Configures structural mappings linking properties to specific tracking target definitions safely. </summary>
        public void AddColumnMaps(IEnumerable<(string columnName, string Alias)> maps)
        {
            foreach (var map in maps)
            {
                if (_columnMaps.ContainsKey(map.columnName))
                {
                    throw new InvalidFilterCriteriaException($"Map With The Name {map.columnName} already Exist!");
                }

                _columnMaps[map.Alias] = map.columnName;
            }
        }

        /// <summary>
        /// Uses reflection to scan the structural properties of your root entity model. 
        /// Populates a cache mapping valid physical columns while safely ignoring excluded attributes.
        /// </summary>
        private HashSet<string> GetColumnName()
        {
            return Root.Type
                .GetProperties()
                .Where(p =>
                    p.GetCustomAttribute<NotMappedAttribute>() == null &&
                    p.GetCustomAttribute<IgnoreAttribute>() == null) // Drops elements flagged for exclusion
                .Select(p => p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name) // Adopts custom names over defaults when provided
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
    }
}
using Dapper;
using SQLQueryBuilder.Attributes;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace SQLQueryBuilder.Model.SQLBuilderModel
{
    internal class SQLInfoModel
    {
        private HashSet<string> _columnNames = new (StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, string> _columnMaps = new(StringComparer.OrdinalIgnoreCase);
        private TableRefModel _root = null!;
        public required TableRefModel Root
        {
            get => _root;
            init
            {
                _root = value;
                _columnNames = GetColumnName();
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
        public string Where {
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
        public DynamicParameters? Parameters { get; set; }
        public string GetMappedColumnName(string columnName)
        {
            if (!_columnNames.TryGetValue(columnName, out var rawName))
            {
                throw new InvalidFilterCriteriaException($"Cannot Find Column With The Name '{columnName}'");
            }

            if (_columnMaps.TryGetValue(rawName, out var colWithAlias))
            {
                return colWithAlias;
            }
            return $"{Root.Alias}.{rawName}";
        }
        public void AddColumnMaps(IEnumerable<(string columnName, string Alias)> maps)
        {
            foreach(var map in maps)
            {
                if (_columnMaps.ContainsKey(map.columnName))
                {
                    throw new InvalidFilterCriteriaException($"Map With The Name {map.columnName} already Exist!");
                }

                _columnMaps[map.Alias] = map.columnName;
            }
        }
        private HashSet<string> GetColumnName()
        {
            return Root.Type
                .GetProperties()
                .Where(p =>
                    p.GetCustomAttribute<NotMappedAttribute>() == null &&
                    p.GetCustomAttribute<IgnoreAttribute>() == null)
                .Select(p => p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
    }
}

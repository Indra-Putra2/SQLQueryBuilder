using Dapper;
using SQLQueryBuilder.Attributes;
using SQLQueryBuilder.Enum;
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
        private TableRefModel _root = null!;

        public TableRefModel Root
        {
            get => _root;
            init
            {
                _root = value;
            }
        }
        private Dictionary<string, List<string>> _validColumns = new Dictionary<string, List<string>>();
        public string _rawSelectedColumns {  get; set; }
        
        private List<ColumnMetadataModel> _parsedSelected = new();
        public List<ColumnMetadataModel> ParsedSelected
        {
            get => _parsedSelected;
            set
            {
                _parsedSelected = value;
            }
        }

        private bool _selectAll = false;

        public void validateSelected(List<ColumnMetadataModel> select)
        {
            foreach (var selected in select)
            {
                // --- 1. EXPRESSION PATHWAY ---
                if (selected.IsExpression)
                {
                    if (selected.ColumnName.Contains("*") && Joins.Count > 0)
                    {
                        PrintWildcardWarning();
                        // Removed 'continue' so we can still validate the alias below if it exists
                    }

                    if (string.IsNullOrWhiteSpace(selected.Alias))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"WARNING! Found an expression \n{selected.ColumnName}\n with no Alias this will not be able to be filtered later!");
                        Console.ResetColor();
                    }
                    else
                    {
                        // Validates against the C# class target
                        CheckValidColumn(selected.Alias);
                    }
                }
                // --- 2. STANDARD COLUMN PATHWAY ---
                else
                {
                    if (selected.ColumnName.Contains("*"))
                    {
                        if (selected.TableAlias != _root.Alias && Joins.Count > 0)
                        {
                            PrintWildcardWarning();
                        }
                        // Wildcards (* or t.*) can't be mapped directly to a single C# property name,
                        // so we skip CheckValidColumn for them entirely.
                        continue;
                    }

                    // Validates Alias if present (C# target), otherwise falls back to ColumnName
                    var propertyToValidate = selected.Alias ?? selected.ColumnName;
                    CheckValidColumn(propertyToValidate);
                }
            }
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
        public OrderTypeEnum orderType { get; set; }

        private string _groupBy = string.Empty;
        public string GroupBy
        {
            get => _groupBy;
            set => _groupBy = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        /// <summary> Parameter state definitions to pass down cleanly into consumer layers like Dapper. </summary>
        public DynamicParameters? Parameters { get; set; }

        public string CheckValidColumn(string columnName)
        {
            // Loop through the column lists in our dictionary
            foreach (var columnList in _validColumns.Values)
            {
                // Use OrdinalIgnoreCase so "Price" matches "price" safely
                var matchedColumn = columnList.FirstOrDefault(c => 
                    string.Equals(c, columnName, StringComparison.OrdinalIgnoreCase));

                // If found, return the exact database-cased name
                if (matchedColumn != null) 
                    return matchedColumn;
            }

            throw new InvalidDataException($"Column with the name '{columnName}' doesn't exist inside the class!");
        }

        internal void BuildValidColums()
        {
            _validColumns[Root.Name] = GetPropertieNames(Root.Type);
            foreach (var join in Joins.Select(j => j.Table))
            {
                _validColumns[join.Name] = GetPropertieNames(join.Type);
            }
        }

        private List<string> GetPropertieNames(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                // Keep properties ONLY if they DO NOT have Ignore or NotMapped
                .Where(p => p.GetCustomAttribute<IgnoreAttribute>() == null
                         && p.GetCustomAttribute<NotMappedAttribute>() == null)
                // Safely check for ColumnAttribute name, fallback to property name
                .Select(p => p.GetCustomAttribute<ColumnAttribute>()?.Name ?? p.Name)
                .ToList();
        }

        private void PrintWildcardWarning()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║ WARNING: You are joining tables and using a wildcard (*) selection.      ║");
            Console.WriteLine("║ SQL will flatten these tables. If they share duplicate column names      ║");
            Console.WriteLine("║ (like 'Id', 'CreatedAt'), your query WILL fail or Dapper will misbind!   ║");
            Console.WriteLine("║ Best Practice: Explicitly select columns with aliases (e.g., t1.Id as X) ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════╝");
            Console.ResetColor();
        }
    }
}
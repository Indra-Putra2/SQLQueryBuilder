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
            set => _orderBy = string.IsNullOrWhiteSpace(value) ? string.Empty : ValidateColumn(value.Trim());
        }
        public OrderTypeEnum OrderType { get; set; }

        private string _groupBy = string.Empty;
        public string GroupBy
        {
            get => _groupBy;
            set => _groupBy = string.IsNullOrWhiteSpace(value) ? string.Empty : ValidateColumn(value.Trim());
        }

        /// <summary> Parameter state definitions to pass down cleanly into consumer layers like Dapper. </summary>
        public DynamicParameters? Parameters { get; set; }

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
                    }

                    if (string.IsNullOrWhiteSpace(selected.Alias))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"WARNING! Found an expression \n{selected.ColumnName}\n with no Alias this will not be able to be filtered later!");
                        Console.ResetColor();
                    }
                    else
                    {
                        ValidateTableAlias(selected.Alias);
                        ValidateColumn(selected.Alias);
                    }
                }
                // --- 2. STANDARD COLUMN PATHWAY ---
                else
                {
                    if (selected.TableAlias != null)
                    {
                        ValidateTableAlias(selected.TableAlias);
                    }

                    if (selected.ColumnName.Contains("*"))
                    {
                        if (selected.TableAlias != _root.Alias && Joins.Count > 0)
                        {
                            PrintWildcardWarning();
                        }
                        continue;
                    }

                    var propertyToValidate = selected.Alias ?? selected.ColumnName;
                    ValidateColumn(propertyToValidate);
                }
            }
        }

        public void ValidateJoin(List<ColumnMetadataModel> joins)
        {
            foreach (var join in joins)
            {
                if(join.TableAlias != null)
                {
                    ValidateTableAlias(join.TableAlias);
                }

                ValidateColumn(join.ColumnName);
            }
        }

        private void ValidateTableAlias(string alias)
        {
            var isRootTable = string.Equals(alias, Root.Alias, StringComparison.InvariantCultureIgnoreCase);

            if (!isRootTable)
            {
                var tableJoinExist = Joins.FirstOrDefault(j => string.Equals(j.Table?.Alias, alias, StringComparison.InvariantCultureIgnoreCase));

                if (tableJoinExist == null)
                {
                    throw new InvalidDataException($"Table with the alias {alias} doesn't match any specified table alias");
                }
            }
        }

        public string ValidateColumn(string columnName)
        {
            foreach (var columnList in _validColumns.Values)
            {
                var matchedColumn = columnList.FirstOrDefault(c => 
                    string.Equals(c, columnName, StringComparison.OrdinalIgnoreCase));

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
                .Where(p => p.GetCustomAttribute<IgnoreAttribute>() == null
                         && p.GetCustomAttribute<NotMappedAttribute>() == null)
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
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

        public List<ColumnMetadataModel> ParsedSelected { get; set; }

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
            set => _orderBy = string.IsNullOrWhiteSpace(value) ? string.Empty : ValidateColumnOrAlias(value.Trim());
        }
        public OrderTypeEnum OrderType { get; set; }

        private string _groupBy = string.Empty;
        public string GroupBy
        {
            get => _groupBy;
            set => _groupBy = string.IsNullOrWhiteSpace(value) ? string.Empty : ValidateColumnOrAlias(value.Trim());
        }

        /// <summary> Parameter state definitions to pass down cleanly into consumer layers like Dapper. </summary>
        public DynamicParameters? Parameters { get; set; }

        public void validateSelected(List<ColumnMetadataModel> select)
        {
            foreach (var selected in select)
            {
                if (selected.ColumnName == "*")
                {
                    throw new InvalidOperationException(
                        "Wildcards (*) are strictly forbidden in this Query Builder.\n" +
                        "To ensure precise Dapper mapping and prevent CTE column conflicts,\n" +
                        "please explicitly select your required columns (e.g., r.Id, b.Name AS BrandName).\n");
                }

                // --- 1. EXPRESSION PATHWAY ---
                if (selected.IsExpression)
                {
                    if (string.IsNullOrWhiteSpace(selected.Alias))
                    {
                        throw new InvalidOperationException($"Found an expression \n{selected.ColumnName}\n with no Alias this will not be able to be filtered later!");
                    }
                    else
                    {
                        selected.Alias = ValidateColumnOrAlias(selected.Alias);
                    }
                }
                // --- 2. STANDARD COLUMN PATHWAY ---
                else
                {
                    if (!string.IsNullOrWhiteSpace(selected.TableAlias))
                    {
                        selected.TableAlias = ValidateTableAlias(selected.TableAlias);

                        if (Joins.Count > 0 && selected.TableAlias != Root.Alias && string.IsNullOrWhiteSpace(selected.Alias))
                        {
                            throw new InvalidDataException(
                                $"Please provide an 'AS' alias for column '{selected.OriginalQuery}' (e.g., {selected.OriginalQuery} AS NewColumn). " +
                                $"Columns from joined tables require an alias so they don't cause duplicate name conflicts in the CTE wrapper!");
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(selected.Alias))
                    {
                        selected.Alias = ValidateColumnOrAlias(selected.Alias);
                    }
                    else
                    {
                        selected.ColumnName = ValidateColumnOrAlias(selected.ColumnName);
                    }
                }
            }
        }

        public void ValidateJoin(List<ColumnMetadataModel> joins)
        {
            foreach (var join in joins)
            {
                if(!string.IsNullOrWhiteSpace(join.TableAlias))
                {
                    join.TableAlias = ValidateTableAlias(join.TableAlias);
                }

                join.ColumnName = ValidateColumnOrAlias(join.ColumnName);
            }
        }

        private string ValidateTableAlias(string alias)
        {
            if (string.Equals(alias, Root.Alias, StringComparison.InvariantCultureIgnoreCase))
            {
                return Root.Alias;
            }
            var match = Joins.FirstOrDefault(j => string.Equals(j.Table?.Alias, alias, StringComparison.InvariantCultureIgnoreCase));
            if (match == null)
            {
                throw new InvalidDataException($"Table alias '{alias}' doesn't match any specified table alias.");
            }
            return match.Table.Alias;
        }

        public string ValidateColumnOrAlias(string columnName)
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

        private void ThrowWildcardError()
        {
            throw new InvalidOperationException(
                """
                    WARNING: You are joining tables and using a wildcard (*) selection.

                    SQL will flatten these tables. If they share duplicate column names
                    (like 'Id', 'CreatedAt'), your query may fail or Dapper may misbind.

                    Best Practice:
                    Explicitly select columns with aliases
                    (e.g., t1.Id AS CustomerId, t2.Id AS OrderId).
                """
            );
        }
    }
}
using Dapper;

namespace SQLQueryBuilder.Model.SQLBuilderModel
{
    internal class SQLInfo
    {
        public required TableRef Root { get; init; }
        private string _selectedColums = null!;
        public string SelectedColumns 
        {
            get => _selectedColums;
            set => _selectedColums = value?.Trim() ?? "*"; 
        }
        public List<JoinInfo> Joins { get; set; } = new();

        private string _where = string.Empty;
        public string Where {
            get => _where;
            set => _where = value.Trim() ?? string.Empty;
        }
        private string _orderBy = string.Empty;
        public string OrderBy
        {
            get => _orderBy;
            set => _orderBy = value.Trim() ?? string.Empty;
        }
        private string _groupBy = string.Empty;
        public string GroupBy
        {
            get => _groupBy;
            set => _groupBy = value.Trim() ?? string.Empty;
        }
        public DynamicParameters? Parameters { get; set; }

        public string GetAlias(Type type)
        {
            if (type == Root.Type) return Root.Alias;
            return Joins.Where(j => j.Table.Type == type).Select(j => j.Table.Alias).FirstOrDefault()!;
        }
    }
}

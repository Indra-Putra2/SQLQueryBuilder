namespace SQLQueryBuilder.Model.QueryBuilder
{
    internal record SQLQueryNew
    {
        public required Type OutputDTO;
        public (string alias, Type type) From = (string.Empty, null);
        public List<JoinInfo> Joins = new();
        public string Select = string.Empty;

        public string? Having { get; set; } = null;
        public string? Where { get; set; } = null;
        public string? GroupBy { get; set; } = null;
        public string? OrderBy { get; set; } = null;
        public int? Limit { get; set; }

        public SQLQueryNew ClearFilter()
        {
            return this with 
            { 
                Having = null,
                Where = null,
                GroupBy = null,
                OrderBy = null,
                Limit = null
            };
        }
    }
}

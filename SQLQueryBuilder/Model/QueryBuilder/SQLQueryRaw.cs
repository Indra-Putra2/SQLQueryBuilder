namespace SQLQueryBuilder.Model.QueryBuilder
{
    internal record SQLQueryRaw
    {
        public required Type OutputDTO;
        public (string alias, Type type) From;
        public List<JoinInfo> Joins = new();
        public string Select = string.Empty;

        public string? Having { get; set; } = null;
        public string? Where { get; set; } = null;
        public string? GroupBy { get; set; } = null;
        public string? OrderBy { get; set; } = null;
        public int? Limit { get; set; }

        public SQLQueryRaw ClearFilter()
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

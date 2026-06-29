using SQLQueryBuilder.Model;

namespace SQLQueryBuilder.Builder
{
    internal class SQLJoinParser
    {
        public List<ColumnMetadataModel> Parse(string join)
        {
            var queue = new List<ColumnMetadataModel>();
            var joinSplit = join.Split("=");
            foreach (var split in joinSplit)
            {
                var result = split.Trim().Split(".");

                var metadata = new ColumnMetadataModel()
                {
                    OriginalQuery = join,
                    TableAlias = result[0],
                    ColumnName = result[1],
                    Alias = string.Empty,
                    IsExpression = false
                };

                queue.Add(metadata);
            }
            return queue;
        }
    }
}

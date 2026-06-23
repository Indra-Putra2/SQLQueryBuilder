using SQLQueryBuilder.Model;

namespace SQLQueryBuilder.SQLBuilder
{
    internal class SQLJoinParser
    {
        public Queue<ColumnMetadataModel> Parse(string join)
        {
            var queue = new Queue<ColumnMetadataModel>();
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

                queue.Enqueue(metadata);
            }
            return queue;
        }
    }
}

namespace SQLQueryBuilder.Model
{
    public enum JoinType
    {
        INNER,
        LEFT,
        RIGHT,
        FULL
    }
    public class JoinInfo
    {
        public required Type Type { get; set; }
        public required string TableName { get; set; }
        public string? Alias { get; set; }
        public HashSet<string> Columns { get; } = new HashSet<string>();
        public JoinType JoinType { get; set; } = JoinType.INNER;
        public string? OnCondition { get; set; }

    }

    public static class JoinTypeExtensions
    {
        public static string ToSql(this JoinType type)
        {
            return type switch
            {
                JoinType.INNER => "INNER JOIN",
                JoinType.LEFT => "LEFT JOIN",
                JoinType.RIGHT => "RIGHT JOIN",
                JoinType.FULL => "FULL JOIN",
                _ => throw new NotSupportedException()
            };
        }
    }
}

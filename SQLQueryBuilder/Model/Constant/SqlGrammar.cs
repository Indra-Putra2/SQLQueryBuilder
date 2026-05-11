namespace SQLQueryBuilder.Model.Constant
{
    internal static class SqlGrammar
    {
        public static readonly HashSet<string> Operators = new()
    {
        "<", ">", ">=", "<=", "=", "!=", ":"
    };
        public const string OperatorChars = "=><!:";
    }
}

namespace SQLQueryBuilder.Model.Constant
{
    public static class SqlGrammar
    {
        public static readonly HashSet<string> Operators = new()
    {
        "<", ">", ">=", "<=", "=", "!=", ":"
    };
        public const string OperatorChars = "=><!:";
    }
}

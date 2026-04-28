namespace SQLQueryBuilder.Model
{
    internal class SQLInfo
    {
        public required TableInfo TableInfo { get; set; }
        public HashSet<string>? SelectedColumn { get; set; }
        public HashSet<JoinInfo>? JoinInfo { get; set; }
    }
}

namespace SQLQueryBuilder.Model
{
    public class ColumnInfo : IEquatable<ColumnInfo>
    {
        public required string Source { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;

        public string ToSql() => string.IsNullOrEmpty(Alias) ? Source : $"{Source} AS {Alias}";
        public bool Equals(ColumnInfo? other) => Source == other?.Source && Alias == other?.Alias;
        public override int GetHashCode() => HashCode.Combine(Source, Alias);
    }
}

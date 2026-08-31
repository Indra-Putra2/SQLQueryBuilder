namespace SQLQueryBuilder.Model
{
    public sealed class Source<T>
    {
        public required T Value { get; init; }
        public required string OriginalText { get; init; }
    }
}

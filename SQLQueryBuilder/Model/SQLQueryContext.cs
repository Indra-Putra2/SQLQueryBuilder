namespace SQLQueryBuilder.Model
{
    public class SQLQueryContext
    {
        public string OriginalText { get; }
        public IReadOnlyList<SQLToken> Tokens { get; }
        public int Index { get; private set; } = 0;

        public SQLQueryContext(string originalText, List<SQLToken> tokens)
        {
            OriginalText = originalText;
            Tokens = tokens;
        }

        // Helper method leveraging ReadOnlySpan!
        public ReadOnlySpan<char> GetSlice(SQLToken token)
            => OriginalText.AsSpan(token.StartPosition, token.Length);
    }
}

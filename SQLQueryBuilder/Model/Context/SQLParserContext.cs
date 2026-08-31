namespace SQLQueryBuilder.Model.Context
{
    public class SQLParserContext
    {
        public string OriginalText { get; }
        public IReadOnlyList<SQLToken> Tokens { get; }

        public SQLParserContext(string originalText, List<SQLToken> tokens)
        {
            OriginalText = originalText;
            Tokens = tokens;
        }

        // Helper method leveraging ReadOnlySpan!
        public ReadOnlySpan<char> GetSlice(SQLToken token)
            => OriginalText.AsSpan(token.StartPosition, token.Length);
    }
}

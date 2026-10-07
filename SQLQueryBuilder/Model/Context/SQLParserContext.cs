using SQLQueryBuilder.Core.SQLEnum.Tokenizer;
using SQLQueryBuilder.Core.Tokenizer;

namespace SQLQueryBuilder.Model.Context
{
    internal ref struct SQLParserContext
    {
        public int Index { get; private set; }
        public ReadOnlySpan<char> Span { get; }
        public IReadOnlyList<SQLToken> Tokens { get; }

#if DEBUG
        public string DebugCurrentText =>
            Current.Type == SQLTokenType.EOF
                ? "<EOF>"
                : GetSlice(Current).ToString();
#endif

        public SQLParserContext(ReadOnlySpan<char> originalText, IReadOnlyList<SQLToken> tokens)
        {
            Span = originalText;
            Tokens = tokens;
            Index = 0;
        }

        public SQLToken Current => (uint)Index < (uint)Span.Length ? Tokens[Index] : Tokens[^1];
        public SQLToken Consume() => (uint)Index < (uint)Span.Length ? Tokens[Index++] : Tokens[^1];
        public bool IsAtEnd => Index >= Tokens.Count;
        public SQLToken Peek(int offset = 1)
        {
            var index = Index + offset;

            var token = (uint)index < (uint)Tokens.Count
                ? Tokens[index]
                : Tokens[^1];

#if DEBUG
            System.Diagnostics.Debug.WriteLine(
                $"{token.Type} [{GetSlice(token).ToString()}]");
#endif

            return token;
        }
        // Helper method leveraging ReadOnlySpan!
        public ReadOnlySpan<char> GetSlice(SQLToken token)
            => Span.Slice(token.StartPosition, token.Length);
    }
}

using SQLQueryBuilder.Core.Tokenizer;

namespace SQLQueryBuilder.Model.Context
{
    internal ref struct SQLTokenizerContext
    {
        public int Index { get; private set; }
        public ReadOnlySpan<char> Span { get; }
        private List<SQLToken> _tokens;
        public IReadOnlyList<SQLToken> Tokens => _tokens;

        public SQLTokenizerContext(ReadOnlySpan<char> input)
        {
            _tokens = new List<SQLToken>();
            Span = input;
            Index = 0;
        }

        public char Current => (uint)Index < (uint)Span.Length ? Span[Index] : '\0';
        public char Consume() => (uint)Index < (uint)Span.Length ? Span[Index++] : '\0';
        public char Peek(int offset = 1)
        {
            var index = Index + offset;

            return (uint)index < (uint)Span.Length
                ? Span[index]
                : '\0';
        }
        public void AdvanceIndex(int count)
        {
            if ((uint)count > (uint)Span.Length - Index)
                throw new ArgumentOutOfRangeException(nameof(count));

            Index += count;
        }
        public void AddToken(SQLToken token) => _tokens.Add(token);
    }
}

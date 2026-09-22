namespace SQLQueryBuilder.Model.Context
{
    internal ref struct SQLTokenizerContext
    {
        public List<SQLToken> Tokens;
        public ReadOnlySpan<char> Span;
        public int Index;

        public SQLTokenizerContext(ReadOnlySpan<char> input)
        {
            Tokens = new List<SQLToken>();
            Span = input;
            Index = 0;
        }

        public char Current =>
            (uint)Index < (uint)Span.Length
                ? Span[Index]
                : '\0';

        public char Peek(int offset = 1)
        {
            var index = Index + offset;

            return (uint)index < (uint)Span.Length
                ? Span[index]
                : '\0';
        }

        public char Consume() => Index < Span.Length ? Span[Index++] : '\0';
        public void AdvanceIndex(int count) => Index += count;
    }
}

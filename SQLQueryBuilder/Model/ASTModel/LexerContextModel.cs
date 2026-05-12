using SQLQueryBuilder.Enum;

namespace SQLQueryBuilder.Model.ASTModel
{
    internal ref struct LexerContextModel
    {
        public readonly ReadOnlySpan<char> Input;
        public int Index { get; private set; } // Encapsulate to prevent manual tampering
        public LexerStateEnum PreviousState { get; private set; } = LexerStateEnum.None;
        public LexerStateEnum CurrentState { get; private set; } = LexerStateEnum.None;
        public LexerContextModel(string input)
        {
            Input = input.AsSpan();
            Index = 0;
        }
        public void SetCurrentState(LexerStateEnum newState)
        {
            PreviousState = CurrentState;
            CurrentState = newState;
        }
        // Peek at the current character without moving the pointer
        public char Peek() => IsAtEnd ? '\0' : Input[Index];

        // Move forward and return character
        public char Consume()
        {
            if (IsAtEnd)
                throw new Exception($"Consume past end at Index={Index}");

            return Input[Index++];
        }
        // Returns a "bookmark" of the current position
        public int Bookmark() => Index;

        // Resets the index to a previous bookmark
        public void Rollback(int bookmark)
        {
            Index = bookmark;
        }
        public bool IsAtEnd => Index >= Input.Length;
    }
}

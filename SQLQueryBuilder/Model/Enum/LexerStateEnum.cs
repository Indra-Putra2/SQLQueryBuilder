namespace SQLQueryBuilder.Model.Enum
{
    public enum LexerStateEnum : byte
    {
        None,
        WhiteSpace,
        Parenthesis,
        Symbol,
        Digit,
        Letter,
    }
}

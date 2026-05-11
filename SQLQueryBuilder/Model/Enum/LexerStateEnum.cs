namespace SQLQueryBuilder.Model.Enum
{
    internal enum LexerStateEnum : byte
    {
        None,
        WhiteSpace,
        Parenthesis,
        Symbol,
        Digit,
        String,
    }
}

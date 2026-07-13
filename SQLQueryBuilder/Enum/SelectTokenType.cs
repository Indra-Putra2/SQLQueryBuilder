namespace SQLQueryBuilder.Enum
{
    internal enum SelectTokenType
    {
        // Special
        EOF,
        Unknown,
        //literals
        Number,
        String,

        // Identifiers
        Identifier,
        Keyword,

        // Quoted identifiers
        BracketIdentifier,   // [Column]
        DoubleQuoteIdentifier, // "Column"
        BacktickIdentifier,  // `Column`

        // Punctuation
        Dot,
        Comma,
        LeftParen,
        RightParen,

        // Operators
        Plus,
        Minus,
        Divide,
        Modulo,

        Equal,
        NotEqual,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,

        // Misc
        Asterisk,
        Semicolon
    }
}

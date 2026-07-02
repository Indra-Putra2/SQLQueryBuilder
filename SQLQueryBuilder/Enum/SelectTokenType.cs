namespace SQLQueryBuilder.Enum
{
    internal enum SelectTokenType
    {
        // Special
        EOF,
        Unknown,

        // Identifiers & literals
        Identifier,
        Keyword,
        Number,
        String,

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

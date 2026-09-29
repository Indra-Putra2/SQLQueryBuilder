namespace SQLQueryBuilder.Core.SQLEnum.Tokenizer
{
    public enum SQLTokenType
    {
        // Special
        EOF,
        Unknown,

        // Literals
        DateLiteral,          // d'2020-05-01' / D'2020-05-01'
        TimeLiteral,          // t'14:30:00' / T'14:30:00'
        DatetimeLiteral,      // dt'2026-08-26 14:30:45'/ DT'2026-08-26 14:30:45' (case insensitive)
        IntegerLiteral,       // 123, -456
        DecimalLiteral,       // 123.45
        ScientificLiteral,    // 1.5E10
        StringLiteral,        // 'Hello'
        UnicodeString,        // N'Unicode text'
        HexLiteral,           // 0x12AF
        MoneyLiteral,         // $100.00
        NullLiteral,

        // Identifiers & Variables
        Identifier,
        Variable,             // @myVar or @@version
        Keyword,

        // Quoted identifiers
        BracketIdentifier,    // [Column]
        DoubleQuoteIdentifier,// "Column"

        // Punctuation
        Dot,
        Comma,
        LeftParen,
        RightParen,
        Semicolon,
        ScopeResolution,      // ::

        // Math & String Operators
        Plus,
        Minus,
        Divide,
        Modulo,
        Asterisk,             // Used for Multiplication OR Wildcard

        // Bitwise Operators
        BitwiseAnd,           // &
        BitwiseOr,            // |
        BitwiseXor,           // ^
        BitwiseNot,           // ~

        BitwiseLeftShift,     // <<
        BitwiseRightShift,    // >>

        // Comparison Operators
        Equal,                // =
        NotEqual,             // <> or !=
        LessThan,             // <
        LessThanOrEqual,      // <=
        GreaterThan,          // >
        GreaterThanOrEqual,   // >=

        // Compound Assignment Operators
        PlusEquals,           // +=
        MinusEquals,          // -=
        MultiplyEquals,       // *=
        DivideEquals,         // /=
        ModuloEquals,         // %=

        // Custom Syntax
        SQLType,              // NUMBER, STRING, NULL etc
        SQLTypeNullable       // ?
    }
}
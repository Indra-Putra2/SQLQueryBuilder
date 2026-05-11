namespace SQLQueryBuilder.Model.ASTModel
{
    internal enum TokenType
    {
        Operator,
        String,
        Number,
        AND,
        OR,
        LParen,
        RParen,
        EOF
    }

    internal class Token
    {
        public required TokenType Type { get; set; }
        public required string Value { get; set; }

        public required int Start { get; set; }
        public required int Length { get; set; }

        public override string ToString()
            => $"{Type}: {Value}";
    }
}

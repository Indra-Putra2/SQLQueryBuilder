namespace SQLQueryBuilder.Model.ASTModel
{
    public enum TokenType
    {
        Operator,
        String,
        Number,
        And,
        Or,
        LParen,
        RParen,
        EOF
    }

    public class Token
    {
        public required TokenType Type { get; set; }
        public required string Value { get; set; }

        public required int Start { get; set; }
        public required int Length { get; set; }

        public override string ToString()
            => $"{Type}: {Value}";
    }
}

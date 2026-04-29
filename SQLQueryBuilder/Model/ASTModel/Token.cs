namespace SQLQueryBuilder.Model.ASTModel
{
    public enum TokenType
    {
        Identifier,
        Operator,
        Value,
        And,
        Or,
        LParen,
        RParen
    }

    public class Token
    {
        public TokenType Type { get; set; }
        public required string Value { get; set; }
        public override string ToString() => $"{Type}: {Value}";
    }
}
